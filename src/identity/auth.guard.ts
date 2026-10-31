import { CanActivate, ExecutionContext, ForbiddenException, Inject, Injectable, UnauthorizedException } from "@nestjs/common";
import type { FastifyRequest } from "fastify";
import { CONFIG } from "../config.token.js";
import type { Config } from "../config.js";
import { IdentityService, type SessionInfo } from "./identity.service.js";

export const SESSION_COOKIE = "nlwc_session";
export const CSRF_COOKIE = "nlwc_csrf";
declare module "fastify" { interface FastifyRequest { session?: SessionInfo } }

/** Authenticates the cookie session. On mutations also enforces Origin allow-list and double-submit CSRF. */
@Injectable()
export class AuthGuard implements CanActivate {
  constructor(@Inject(IdentityService) private identity: IdentityService, @Inject(CONFIG) private config: Config) {}

  async canActivate(ctx: ExecutionContext): Promise<boolean> {
    const req = ctx.switchToHttp().getRequest<FastifyRequest>();
    const token = req.cookies[SESSION_COOKIE];
    const session = token ? await this.identity.resolve(token) : null;
    if (!session) throw new UnauthorizedException();
    if (!["GET", "HEAD", "OPTIONS"].includes(req.method)) {
      const origin = req.headers.origin;
      const csrf = req.headers["x-csrf-token"];
      if (!origin || !this.config.allowedOrigins.includes(origin)) throw new ForbiddenException("bad_origin");
      if (!csrf || csrf !== req.cookies[CSRF_COOKIE]) throw new ForbiddenException("bad_csrf");
    }
    req.session = session;
    return true;
  }
}
