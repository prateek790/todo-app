import {
  AngularNodeAppEngine,
  createNodeRequestHandler,
  isMainModule,
  writeResponseToNodeResponse,
} from '@angular/ssr/node';
import express from 'express';
import { request as httpRequest } from 'node:http';
import { request as httpsRequest } from 'node:https';
import { join } from 'node:path';

const browserDistFolder = join(import.meta.dirname, '../browser');

const app = express();
const angularApp = new AngularNodeAppEngine();
const apiTarget = new URL(
  process.env['API_HTTPS'] || process.env['API_HTTP'] || 'https://localhost:7228',
);

app.use('/api', (req, res, next) => {
  const requestOptions = {
    hostname: apiTarget.hostname,
    port: apiTarget.port,
    method: req.method,
    path: `${apiTarget.pathname.replace(/\/$/, '')}${req.originalUrl}`,
    headers: { ...req.headers, host: apiTarget.host },
  };
  const handleResponse = (apiResponse: import('node:http').IncomingMessage) => {
    res.writeHead(apiResponse.statusCode ?? 502, apiResponse.headers);
    apiResponse.pipe(res);
  };
  const localDevelopmentTarget = ['localhost', '127.0.0.1', '::1'].includes(apiTarget.hostname);
  const proxyRequest = apiTarget.protocol === 'https:'
    ? httpsRequest(
        { ...requestOptions, rejectUnauthorized: !localDevelopmentTarget },
        handleResponse,
      )
    : httpRequest(requestOptions, handleResponse);

  proxyRequest.on('error', next);
  req.pipe(proxyRequest);
});

/**
 * Example Express Rest API endpoints can be defined here.
 * Uncomment and define endpoints as necessary.
 *
 * Example:
 * ```ts
 * app.get('/api/{*splat}', (req, res) => {
 *   // Handle API request
 * });
 * ```
 */

/**
 * Serve static files from /browser
 */
app.use(
  express.static(browserDistFolder, {
    maxAge: '1y',
    index: false,
    redirect: false,
  }),
);

/**
 * Handle all other requests by rendering the Angular application.
 */
app.use((req, res, next) => {
  angularApp
    .handle(req)
    .then((response) =>
      response ? writeResponseToNodeResponse(response, res) : next(),
    )
    .catch(next);
});

/**
 * Start the server if this module is the main entry point, or it is ran via PM2.
 * The server listens on the port defined by the `PORT` environment variable, or defaults to 4200.
 */
if (isMainModule(import.meta.url) || process.env['pm_id']) {
  const port = process.env['PORT'] || 4200;
  app.listen(port, (error) => {
    if (error) {
      throw error;
    }

    console.log(`Node Express server listening on http://localhost:${port}`);
  });
}

/**
 * Request handler used by the Angular CLI (for dev-server and during build) or Firebase Cloud Functions.
 */
export const reqHandler = createNodeRequestHandler(app);
