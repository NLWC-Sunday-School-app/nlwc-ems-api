import { Controller, Get } from "@nestjs/common";
import type { HealthResponse } from "@nlwc/contracts";

@Controller("v1/health")
export class HealthController {
  @Get()
  get(): HealthResponse {
    return { status: "ok", time: new Date().toISOString() };
  }
}
