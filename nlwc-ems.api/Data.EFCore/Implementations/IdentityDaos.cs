using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using nlwc.ems.Core.Models;

namespace nlwc.ems.Data.EFCore.Implementations;

public class PersonDao(EmsDbContext dbContext, IHttpContextAccessor httpContextAccessor, IAuditEventPublisher auditEventPublisher)
    : BaseDao<EmsDbContext, Person>(dbContext, httpContextAccessor, auditEventPublisher), IPersonDao;

public class AccountDao(EmsDbContext dbContext, IHttpContextAccessor httpContextAccessor, IAuditEventPublisher auditEventPublisher)
    : BaseDao<EmsDbContext, Account>(dbContext, httpContextAccessor, auditEventPublisher), IAccountDao
{
    public Task<Account?> RetrieveByEmailAsync(string email) => Query().FirstOrDefaultAsync(a => a.Email == email);

    public async Task<AccountWithPerson?> RetrieveWithPersonByEmailAsync(string email)
    {
        var found = await (from a in Query()
                           join p in DbContext.Set<Person>().AsNoTracking() on a.PersonId equals p.Id
                           where a.Email == email
                           select new { Account = a, Person = p }).FirstOrDefaultAsync();
        return found is null ? null : new AccountWithPerson(found.Account, found.Person);
    }

    public Task<bool> EmailExistsAsync(string email) => DbContext.Set<Account>().AnyAsync(a => a.Email == email);

    public Task InsertWithPersonAsync(Person person, Account account, VerifiedContact contact, EmailVerification verification)
        => InTransactionAsync(async () =>
        {
            foreach (var e in new BaseEntity[] { person, account, contact, verification }) StampNew(e);
            DbContext.Set<Person>().Add(person);
            DbContext.Set<Account>().Add(account);
            DbContext.Set<VerifiedContact>().Add(contact);
            DbContext.Set<EmailVerification>().Add(verification);
            await DbContext.SaveChangesAsync();
        });

    public async Task RegisterFailedLoginAsync(Guid accountId, int maxFailures, DateTime lockUntil)
    {
        var now = DateTime.UtcNow;
        await DbContext.Set<Account>().Where(a => a.Id == accountId)
            .ExecuteUpdateAsync(set => set.SetProperty(a => a.FailedLogins, a => a.FailedLogins + 1).SetProperty(a => a.DateModified, now));
        await DbContext.Set<Account>().Where(a => a.Id == accountId && a.FailedLogins >= maxFailures)
            .ExecuteUpdateAsync(set => set.SetProperty(a => a.LockedUntil, lockUntil));
    }

    public Task<Account?> RetrieveByGoogleSubAsync(string sub) => Query().FirstOrDefaultAsync(a => a.GoogleSub == sub);

    public Task<bool> HasVerifiedEmailAsync(Guid accountId)
        => (from c in DbContext.Set<VerifiedContact>()
            join a in DbContext.Set<Account>() on c.PersonId equals a.PersonId
            where a.Id == accountId && c.Kind == ContactKind.Email && c.Value == a.Email && c.VerifiedAt != null
            select c.Id).AnyAsync();

    public async Task<bool> LinkGoogleAsync(Guid accountId, string sub)
    {
        var now = DateTime.UtcNow;
        try
        {
            return await DbContext.Set<Account>().Where(a => a.Id == accountId && a.GoogleSub == null)
                .ExecuteUpdateAsync(set => set.SetProperty(a => a.GoogleSub, sub).SetProperty(a => a.DateModified, now)) == 1;
        }
        catch (DbUpdateException)
        {
            return false; // the unique index: this Google identity already belongs to another account
        }
    }

    public Task InsertGoogleAccountAsync(Person person, Account account, VerifiedContact verifiedContact)
        => InTransactionAsync(async () =>
        {
            foreach (var e in new BaseEntity[] { person, account, verifiedContact }) StampNew(e);
            DbContext.Set<Person>().Add(person);
            DbContext.Set<Account>().Add(account);
            DbContext.Set<VerifiedContact>().Add(verifiedContact);
            await DbContext.SaveChangesAsync();
        });

    public Task ResetFailedLoginsAsync(Guid accountId)
        => DbContext.Set<Account>().Where(a => a.Id == accountId && (a.FailedLogins != 0 || a.LockedUntil != null))
            .ExecuteUpdateAsync(set => set.SetProperty(a => a.FailedLogins, 0).SetProperty(a => a.LockedUntil, (DateTime?)null));
}

public class EmailVerificationDao(EmsDbContext dbContext, IHttpContextAccessor httpContextAccessor, IAuditEventPublisher auditEventPublisher)
    : BaseDao<EmsDbContext, EmailVerification>(dbContext, httpContextAccessor, auditEventPublisher), IEmailVerificationDao
{
    public async Task<bool> ConsumeAsync(string tokenHash)
    {
        var consumed = false;
        await InTransactionAsync(async () =>
        {
            var now = DateTime.UtcNow;
            var row = await Query().Where(v => v.TokenHash == tokenHash && v.UsedAt == null && v.ExpiresAt > now)
                .Select(v => new { v.Id, v.AccountId }).FirstOrDefaultAsync();
            if (row is null) return;

            // The used_at IS NULL guard makes a double-click or replay lose the race rather than verify twice.
            var claimed = await DbContext.Set<EmailVerification>().Where(v => v.Id == row.Id && v.UsedAt == null)
                .ExecuteUpdateAsync(set => set.SetProperty(v => v.UsedAt, now).SetProperty(v => v.DateModified, now));
            if (claimed == 0) return;

            await (from c in DbContext.Set<VerifiedContact>()
                   join a in DbContext.Set<Account>() on c.PersonId equals a.PersonId
                   where a.Id == row.AccountId && c.Kind == ContactKind.Email && c.Value == a.Email
                   select c)
                .ExecuteUpdateAsync(set => set.SetProperty(c => c.VerifiedAt, now).SetProperty(c => c.DateModified, now));
            consumed = true;
        });
        return consumed;
    }
}

public class AuthSessionDao(EmsDbContext dbContext, IHttpContextAccessor httpContextAccessor, IAuditEventPublisher auditEventPublisher)
    : BaseDao<EmsDbContext, AuthSession>(dbContext, httpContextAccessor, auditEventPublisher), IAuthSessionDao
{
    public async Task<ResolvedSession?> RetrieveActiveAsync(string tokenHash)
    {
        var now = DateTime.UtcNow;
        return await (from s in Query()
                      join a in DbContext.Set<Account>().AsNoTracking() on s.AccountId equals a.Id
                      where s.TokenHash == tokenHash && s.RevokedAt == null && s.IdleExpiresAt > now && s.AbsoluteExpiresAt > now
                      select new ResolvedSession
                      {
                          Session = s,
                          PersonId = a.PersonId,
                          Email = a.Email,
                          EmailVerified = DbContext.Set<VerifiedContact>().Any(c =>
                              c.PersonId == a.PersonId && c.Kind == ContactKind.Email && c.Value == a.Email && c.VerifiedAt != null)
                      }).FirstOrDefaultAsync();
    }

    public Task TouchAsync(Guid sessionId, DateTime idleExpiresAt)
    {
        var now = DateTime.UtcNow;
        return DbContext.Set<AuthSession>().Where(s => s.Id == sessionId)
            .ExecuteUpdateAsync(set => set.SetProperty(s => s.LastSeenAt, now).SetProperty(s => s.IdleExpiresAt, idleExpiresAt));
    }

    public Task RevokeAsync(Guid sessionId)
    {
        var now = DateTime.UtcNow;
        return DbContext.Set<AuthSession>().Where(s => s.Id == sessionId)
            .ExecuteUpdateAsync(set => set.SetProperty(s => s.RevokedAt, now).SetProperty(s => s.DateModified, now));
    }

    public Task MarkMfaVerifiedAsync(Guid sessionId)
    {
        var now = DateTime.UtcNow;
        return DbContext.Set<AuthSession>().Where(s => s.Id == sessionId)
            .ExecuteUpdateAsync(set => set.SetProperty(s => s.MfaVerifiedAt, now).SetProperty(s => s.DateModified, now));
    }
}

public class MfaFactorDao(EmsDbContext dbContext, IHttpContextAccessor httpContextAccessor, IAuditEventPublisher auditEventPublisher)
    : BaseDao<EmsDbContext, MfaFactor>(dbContext, httpContextAccessor, auditEventPublisher), IMfaFactorDao
{
    public Task<MfaFactor?> RetrieveLatestAsync(Guid accountId)
        => Query().Where(f => f.AccountId == accountId)
            .OrderByDescending(f => f.ConfirmedAt != null).ThenByDescending(f => f.DateCreated)
            .FirstOrDefaultAsync();

    public Task ReplacePendingAsync(MfaFactor factor)
        => InTransactionAsync(async () =>
        {
            await DbContext.Set<MfaFactor>().Where(f => f.AccountId == factor.AccountId && f.ConfirmedAt == null).ExecuteDeleteAsync();
            await InsertAsync(factor);
        });

    public Task<bool> HasConfirmedAsync(Guid accountId) => Query().AnyAsync(f => f.AccountId == accountId && f.ConfirmedAt != null);

    public Task ConfirmAsync(Guid factorId)
    {
        var now = DateTime.UtcNow;
        return DbContext.Set<MfaFactor>().Where(f => f.Id == factorId)
            .ExecuteUpdateAsync(set => set.SetProperty(f => f.ConfirmedAt, now).SetProperty(f => f.DateModified, now));
    }
}
