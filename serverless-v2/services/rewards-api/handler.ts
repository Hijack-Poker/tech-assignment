import 'reflect-metadata';
import { NestFactory } from '@nestjs/core';
import { ValidationPipe } from '@nestjs/common';
import { ExpressAdapter } from '@nestjs/platform-express';
import { AppModule } from './src/app.module';
import { NotFoundFilter } from './src/filters/not-found.filter';

// CJS modules — use require to avoid type resolution issues
// eslint-disable-next-line @typescript-eslint/no-var-requires
const serverless = require('serverless-http');
// eslint-disable-next-line @typescript-eslint/no-var-requires
const express = require('express');

type Handler = (event: unknown, context: unknown) => Promise<unknown>;

let cachedHandler: Handler;

async function bootstrap(): Promise<Handler> {
  const expressApp = express();
  const app = await NestFactory.create(AppModule, new ExpressAdapter(expressApp), {
    logger: ['error', 'warn'],
  });

  app.setGlobalPrefix('api/v1');
  app.enableCors({
    origin: '*',
    allowedHeaders: ['Content-Type', 'Authorization', 'X-Player-Id'],
    methods: ['GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'OPTIONS'],
  });
  app.useGlobalPipes(
    new ValidationPipe({
      whitelist: true,
      forbidNonWhitelisted: true,
      transform: true,
    }),
  );
  app.useGlobalFilters(new NotFoundFilter());

  await app.init();

  return serverless(expressApp);
}

export const api = async (event: unknown, context: unknown): Promise<unknown> => {
  if (!cachedHandler) {
    cachedHandler = await bootstrap();
  }
  return cachedHandler(event, context);
};
