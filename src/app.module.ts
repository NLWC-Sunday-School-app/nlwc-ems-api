import { Global, Module, type DynamicModule } from "@nestjs/common";
import type { Kysely } from "kysely";
import type { Config } from "./config.js";
import { CONFIG } from "./config.token.js";
import { DB_TOKEN } from "./db/db.js";
import type { DB } from "./db/types.js";
import { HealthController } from "./health.controller.js";
import { IdentityModule } from "./identity/identity.module.js";

@Global()
@Module({})
class CoreModule {
  static forRoot(config: Config, db: Kysely<DB>): DynamicModule {
    return {
      module: CoreModule,
      providers: [{ provide: CONFIG, useValue: config }, { provide: DB_TOKEN, useValue: db }],
      exports: [CONFIG, DB_TOKEN],
    };
  }
}

@Module({})
export class AppModule {
  static forRoot(config: Config, db: Kysely<DB>): DynamicModule {
    return { module: AppModule, imports: [CoreModule.forRoot(config, db), IdentityModule], controllers: [HealthController] };
  }
}
