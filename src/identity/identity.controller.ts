import { Body, Controller, Get, HttpCode, Post, Req, Res, UseGuards, UnauthorizedException, BadRequestException } from "@nestjs/common";
import type { FastifyReply, FastifyRequest } from "fastify";
import { z } from "zod";
import { Inject } from "@nestjs/common";
import { CONFIG } from "../config.token.js";
import type { Config } from "../config.js";
import { AuthGuard, CSRF_COOKIE, SESSION_COOKIE } from "./auth.guard.js";
import { AuthError, IdentityService, type Surface } from "./identity.service.js";
import { newToken } from "./crypto.js";

const SignUp = z.object({
  email: z.string().email(), password: z.string().min(10).max(200),
  givenName: z.string().min(1).max(100), familyName: z.string().min(1).max(100),
  gender: z.enum(["male", "female"]).optional(),
});
const LogIn = z.object({ email: z.string().email(), password: z.string().max(200), surface: z.enum(["web", "admin", "checkin"]).default("web") });
const Code = z.object({ code: z.string().regex(/^\d{6}$/) });

function parse<T>(schema: z.ZodType<T>, body: unknown): T {
  const r = schema.safeParse(body);
  if (!r.success) throw new BadRequestException({ code: "validation_failed", issues: r.error.issues.map((i) => ({ path: i.path.join("."), message: i.message })) });
  return r.data;
}

@Controller("v1/auth")
export class IdentityController {
  constructor(@Inject(IdentityService) private identity: IdentityService, @Inject(CONFIG) private config: Config) {}

  @Post("signup") @HttpCode(202)
  async signUp(@Body() body: unknown) {
    await this.identity.signUp(parse(SignUp, body));
    return { status: "check_your_email" };
  }

  @Post("verify-email") @HttpCode(204)
  async verifyEmail(@Body() body: unknown) {
    try { await this.identity.verifyEmail(parse(z.object({ token: z.string().min(10) }), body).token); }
    catch (e) { if (e instanceof AuthError) throw new BadRequestException({ code: e.code }); throw e; }
  }

  @Post("login") @HttpCode(200)
  async logIn(@Body() body: unknown, @Req() req: FastifyRequest, @Res({ passthrough: true }) reply: FastifyReply) {
    const b = parse(LogIn, body);
    const origin = req.headers.origin;
    if (!origin || !this.config.allowedOrigins.includes(origin)) throw new BadRequestException({ code: "bad_origin" });
    try {
      const { token, session } = await this.identity.logIn(b.email, b.password, b.surface as Surface);
      const base = { path: "/", secure: this.config.cookieSecure, sameSite: "lax" as const };
      reply.setCookie(SESSION_COOKIE, token, { ...base, httpOnly: true });
      const csrf = newToken();
      reply.setCookie(CSRF_COOKIE, csrf, { ...base, httpOnly: false });
      return { csrfToken: csrf, mfaVerified: session.mfaVerified, emailVerified: session.emailVerified };
    } catch (e) {
      if (e instanceof AuthError) throw new UnauthorizedException({ code: e.code });
      throw e;
    }
  }

  @Post("logout") @HttpCode(204) @UseGuards(AuthGuard)
  async logOut(@Req() req: FastifyRequest, @Res({ passthrough: true }) reply: FastifyReply) {
    await this.identity.logOut(req.session!.sessionId);
    reply.clearCookie(SESSION_COOKIE, { path: "/" });
    reply.clearCookie(CSRF_COOKIE, { path: "/" });
  }

  @Get("me") @UseGuards(AuthGuard)
  me(@Req() req: FastifyRequest) {
    const s = req.session!;
    return { personId: s.personId, surface: s.surface, mfaVerified: s.mfaVerified, emailVerified: s.emailVerified };
  }

  @Post("mfa/enroll") @UseGuards(AuthGuard)
  enroll(@Req() req: FastifyRequest) { return this.identity.mfaEnroll(req.session!); }

  @Post("mfa/verify") @HttpCode(204) @UseGuards(AuthGuard)
  async verify(@Req() req: FastifyRequest, @Body() body: unknown) {
    try { await this.identity.mfaVerify(req.session!, parse(Code, body).code); }
    catch (e) { if (e instanceof AuthError) throw new BadRequestException({ code: e.code }); throw e; }
  }
}
