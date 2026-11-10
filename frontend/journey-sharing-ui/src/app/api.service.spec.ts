import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApiService } from './api.service';

describe('ApiService session and chat', () => {
  let api: ApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(ApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => { http.verify(); sessionStorage.clear(); });

  it('uses the login token for the profile and clears it on sign out', async () => {
    const login = api.login('ban@congty.com', 'a-long-password');
    http.expectOne('http://localhost:8080/api/v1/auth/login').flush({
      accessToken: 'test-access-token', expiresAt: '2099-01-01T00:00:00Z'
    });
    await Promise.resolve();
    const profile = http.expectOne('http://localhost:8080/api/v1/users/me');
    expect(profile.request.headers.get('Authorization')).toBe('Bearer test-access-token');
    profile.flush({ id: 'person-1', fullName: 'An', email: 'ban@congty.com' });
    await login;
    expect(api.profile()?.fullName).toBe('An');
    expect(sessionStorage.getItem('journey-session')).toContain('test-access-token');
    api.logout();
    expect(api.profile()).toBeNull();
    expect(sessionStorage.getItem('journey-session')).toBeNull();
  });

  it('requires the login token when reading and sending chat messages', async () => {
    api.token.set('test-access-token');
    const messages = api.messages('connection-1');
    const get = http.expectOne('http://localhost:8080/api/v1/connections/connection-1/messages');
    expect(get.request.headers.get('Authorization')).toBe('Bearer test-access-token');
    get.flush([]);
    expect(await messages).toEqual([]);

    const send = api.sendMessage('connection-1', 'Xin chào');
    const post = http.expectOne('http://localhost:8080/api/v1/connections/connection-1/messages');
    expect(post.request.method).toBe('POST');
    expect(post.request.headers.get('Authorization')).toBe('Bearer test-access-token');
    expect(post.request.body).toEqual({ text: 'Xin chào' });
    post.flush({ id: 'message-1', senderId: 'person-1', text: 'Xin chào', createdAt: '2026-11-10T00:00:00Z' });
    expect((await send).text).toBe('Xin chào');
  });
});
