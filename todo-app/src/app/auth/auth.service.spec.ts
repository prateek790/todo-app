import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthService } from './auth.service';
import { AuthSession } from './auth.models';

describe('AuthService', () => {
  let service: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), AuthService],
    });
    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    sessionStorage.clear();
  });

  it('stores a successful login and clears the session on logout', () => {
    const session: AuthSession = {
      accessToken: 'signed-token',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      user: {
        id: 8,
        name: 'Taylor Reed',
        email: 'taylor@example.com',
        userType: 'User',
      },
    };

    service.login({ email: session.user.email, password: 'a-strong-password' }).subscribe();
    http.expectOne('/api/auth/login').flush(session);

    expect(service.isAuthenticated()).toBe(true);
    expect(service.currentUser()?.name).toBe('Taylor Reed');
    expect(sessionStorage.getItem('peopledesk.auth')).toContain('signed-token');

    service.logout().subscribe();
    http.expectOne('/api/auth/logout').flush(null);

    expect(service.isAuthenticated()).toBe(false);
    expect(sessionStorage.getItem('peopledesk.auth')).toBeNull();
  });

  it('registers an account and stores the returned session', () => {
    const session: AuthSession = {
      accessToken: 'registered-token',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      user: {
        id: 9,
        name: 'Jordan Lee',
        email: 'jordan@example.com',
        userType: 'User',
      },
    };

    service.register({
      name: 'Jordan Lee',
      email: 'jordan@example.com',
      mobile: '5551234567',
      gender: 'Non-binary',
      dob: '1995-04-12',
      password: 'a-strong-password',
    }).subscribe();
    http.expectOne('/api/auth/register').flush(session, { status: 201, statusText: 'Created' });

    expect(service.isAuthenticated()).toBe(true);
    expect(service.currentUser()?.id).toBe(9);
    expect(sessionStorage.getItem('peopledesk.auth')).toContain('registered-token');
  });
});
