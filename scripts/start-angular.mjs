import { spawn } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const angularCli = fileURLToPath(new URL('../node_modules/@angular/cli/bin/ng.js', import.meta.url));
const port = process.env.PORT || '4200';
const server = spawn(
  process.execPath,
  [angularCli, 'serve', '--host', '0.0.0.0', '--port', port, '--proxy-config', 'proxy.conf.cjs'],
  { stdio: 'inherit' },
);

for (const signal of ['SIGINT', 'SIGTERM']) {
  process.on(signal, () => server.kill(signal));
}

server.on('error', (error) => {
  console.error('Unable to start the Angular development server:', error);
  process.exitCode = 1;
});

server.on('exit', (code, signal) => {
  process.exitCode = signal ? 1 : (code ?? 1);
});
