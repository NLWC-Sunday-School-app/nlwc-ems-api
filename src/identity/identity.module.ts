import { Module } from "@nestjs/common";
import { AuthGuard } from "./auth.guard.js";
import { IdentityController } from "./identity.controller.js";
import { IdentityService } from "./identity.service.js";
import { ConsoleMailer, MAILER } from "./mailer.js";

@Module({
  controllers: [IdentityController],
  providers: [IdentityService, AuthGuard, { provide: MAILER, useClass: ConsoleMailer }],
  exports: [IdentityService, AuthGuard],
})
export class IdentityModule {}
