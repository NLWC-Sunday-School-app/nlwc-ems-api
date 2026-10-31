import "reflect-metadata";
import { NestFactory } from "@nestjs/core";
import { FastifyAdapter, type NestFastifyApplication } from "@nestjs/platform-fastify";
import fastifyCookie from "@fastify/cookie";
import type { Kysely } from "kysely";
import { AppModule } from "./app.module.js";
import type { Config } from "./config.js";
import type { DB } from "./db/types.js";

export async function createApp(config: Config, db: Kysely<DB>) {
  const app = await NestFactory.create<NestFastifyApplication>(AppModule.forRoot(config, db), new FastifyAdapter({ bodyLimit: 100_000 }), { logger: process.env.NODE_ENV === "test" ? false : undefined });
  await app.register(fastifyCookie as never);
  app.enableCors({ origin: config.allowedOrigins, credentials: true });
  return app;
}
