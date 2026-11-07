import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { computed, Inject, Injectable, PLATFORM_ID, signal, WritableSignal } from '@angular/core';
import { finalize, Observable, tap } from 'rxjs';
import { AuthSession, AuthUser, LoginRequest, RegisterRequest } from './auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly storageKey = 'peopledesk.auth';
  private readonly session: WritableSignal<AuthSession | null>;

  readonly currentUser = computed(() => this.validSession()?.user ?? null);
  readonly initials = computed(() => {
    const name = this.currentUser()?.name.trim();
    return name ? name.split(/\s+/).slice(0, 2).map((part) => part[0]).join('').toUpperCase() : '';
  });

  constructor(
    private readonly http: HttpClient,
    @Inject(PLATFORM_ID) private readonly platformId: object,
  ) {
    this.session = signal(this.readSession());
  }

  accessToken(): string | null {
    return this.validSession()?.accessToken ?? null;
  }

  isAuthenticated(): boolean {
    return this.validSession() !== null;
  }

  login(credentials: LoginRequest): Observable<AuthSession> {
    return this.http.post<AuthSession>('/api/auth/login', credentials).pipe(
      tap((session) => this.saveSession(session)),
    );
  }

  register(request: RegisterRequest): Observable<AuthSession> {
    return this.http.post<AuthSession>('/api/auth/register', request).pipe(
      tap((session) => this.saveSession(session)),
    );
  }

  logout(): Observable<void> {
    return this.http.post<void>('/api/auth/logout', {}).pipe(
      finalize(() => this.clearSession()),
    );
  }

  clearSession(): void {
    this.session.set(null);
    if (isPlatformBrowser(this.platformId)) {
      sessionStorage.removeItem(this.storageKey);
    }
  }

  private validSession(): AuthSession | null {
    const session = this.session();
    if (!session) {
      return null;
    }

    const expiresAt = Date.parse(session.expiresAt);
    if (!Number.isFinite(expiresAt) || expiresAt <= Date.now()) {
      this.clearSession();
      return null;
    }

    return session;
  }

  private saveSession(session: AuthSession): void {
    this.session.set(session);
    if (isPlatformBrowser(this.platformId)) {
      sessionStorage.setItem(this.storageKey, JSON.stringify(session));
    }
  }

  private readSession(): AuthSession | null {
    if (!isPlatformBrowser(this.platformId)) {
      return null;
    }

    const stored = sessionStorage.getItem(this.storageKey);
    if (!stored) {
      return null;
    }

    let session: unknown;
    try {
      session = JSON.parse(stored) as unknown;
    } catch (error) {
      if (!(error instanceof SyntaxError)) {
        throw error;
      }

      sessionStorage.removeItem(this.storageKey);
      return null;
    }

    if (typeof session !== 'object' || session === null) {
      sessionStorage.removeItem(this.storageKey);
      return null;
    }

    const storedSession = session as Partial<AuthSession>;
    if (
      typeof storedSession.accessToken !== 'string' ||
      typeof storedSession.expiresAt !== 'string' ||
      !Number.isFinite(Date.parse(storedSession.expiresAt)) ||
      Date.parse(storedSession.expiresAt) <= Date.now() ||
      typeof storedSession.user !== 'object' ||
      storedSession.user === null ||
      typeof storedSession.user.id !== 'number' ||
      typeof storedSession.user.name !== 'string' ||
      typeof storedSession.user.email !== 'string' ||
      typeof storedSession.user.userType !== 'string'
    ) {
      sessionStorage.removeItem(this.storageKey);
      return null;
    }

    return storedSession as AuthSession;
  }
}
