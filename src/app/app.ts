import { Component, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './auth/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  readonly profileMenuOpen = signal(false);
  readonly logoutError = signal(false);

  constructor(
    readonly auth: AuthService,
    private readonly router: Router,
  ) {}

  toggleProfileMenu(): void {
    this.profileMenuOpen.update((open) => !open);
  }

  logout(): void {
    this.profileMenuOpen.set(false);
    this.logoutError.set(false);
    this.auth.logout().subscribe({
      next: () => void this.router.navigate(['/login']),
      error: () => {
        this.logoutError.set(true);
        void this.router.navigate(['/login']);
      },
    });
  }
}
