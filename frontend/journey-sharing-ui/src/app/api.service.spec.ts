import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApiService } from './api.service';
import { vi } from 'vitest';

describe('ApiService session and chat', () => {
  let api: ApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(ApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => { http.verify(); api.logout(); vi.useRealTimers(); sessionStorage.clear(); });

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

  it('clears the account and saved session when the access token expires', async () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-11-11T00:00:00Z'));
    const login = api.login('ban@congty.com', 'a-long-password');
    http.expectOne('http://localhost:8080/api/v1/auth/login').flush({
      accessToken: 'short-token', expiresAt: '2026-11-11T00:00:05Z'
    });
    await Promise.resolve();
    http.expectOne('http://localhost:8080/api/v1/users/me').flush({
      id: 'person-1', fullName: 'An', email: 'ban@congty.com'
    });
    await login;
    expect(api.profile()?.fullName).toBe('An');
    vi.advanceTimersByTime(5000);
    expect(api.profile()).toBeNull();
    expect(sessionStorage.getItem('journey-session')).toBeNull();
  });

  it('clears an invalid session after a protected request returns 401', async () => {
    api.token.set('invalid-token');
    api.profile.set({ id: 'person-1', fullName: 'An', email: 'ban@congty.com' });
    const request = api.myTrips().catch(error => error);
    http.expectOne('http://localhost:8080/api/v1/trips/me').flush(
      { error: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });
    await request;
    expect(api.profile()).toBeNull();
    expect(api.token()).toBe('');
  });

  it('updates the signed-in profile with the supplied name and phone', async () => {
    api.token.set('profile-token');
    const update = api.updateProfile({ fullName: 'An Nguyễn', phone: '0912345678' });
    const request = http.expectOne('http://localhost:8080/api/v1/users/me');
    expect(request.request.method).toBe('PATCH');
    expect(request.request.headers.get('Authorization')).toBe('Bearer profile-token');
    expect(request.request.body).toEqual({ fullName: 'An Nguyễn', phone: '0912345678' });
    request.flush({ id: 'person-1', fullName: 'An Nguyễn', email: 'ban@congty.com', phone: '0912345678' });
    await update;
    expect(api.profile()?.phone).toBe('0912345678');
  });
});
