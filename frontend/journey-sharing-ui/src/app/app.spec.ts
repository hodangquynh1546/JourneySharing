import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { App } from './app';
import { vi } from 'vitest';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    sessionStorage.clear();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should show discovery and its empty results', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('http://localhost:8080/api/v1/trips').flush([]);
    http.expectOne('http://localhost:8080/api/v1/travel-requests').flush([]);
    await new Promise(resolve => setTimeout(resolve, 10));
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Mỗi hành trình');
    expect(compiled.textContent).toContain('Chưa có hành trình phù hợp');
    http.verify();
  });

  it('refreshes an open chat and stops polling after it closes', async () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    const http = TestBed.inject(HttpTestingController);
    const timer = vi.spyOn(window, 'setInterval');
    const clearTimer = vi.spyOn(window, 'clearInterval');
    const connection = { id: 'connection-1', tripId: 'trip-1', senderId: 'person-1',
      status: 'Accepted', reservedSeats: 1, acceptedAt: '2026-11-10T00:00:00Z' };
    const opened = app.openChat(connection);
    http.expectOne('http://localhost:8080/api/v1/connections/connection-1/messages').flush([]);
    await opened;
    expect(timer).toHaveBeenCalledWith(expect.any(Function), 5000);
    app.closeChat();
    expect(clearTimer).toHaveBeenCalled();
    expect(app.activeChat()).toBeNull();
    fixture.destroy();
    timer.mockRestore();
    clearTimer.mockRestore();
    http.verify();
  });

  it('does not show messages from a chat that was replaced before loading', async () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    const http = TestBed.inject(HttpTestingController);
    const first = app.openChat({ id: 'first', tripId: 'trip-1', senderId: 'person-1',
      status: 'Accepted', reservedSeats: 1 });
    const oldRequest = http.expectOne('http://localhost:8080/api/v1/connections/first/messages');
    const second = app.openChat({ id: 'second', tripId: 'trip-2', senderId: 'person-2',
      status: 'Accepted', reservedSeats: 1 });
    http.expectOne('http://localhost:8080/api/v1/connections/second/messages').flush([
      { id: 'new', senderId: 'person-2', text: 'Tin mới', createdAt: '2026-11-10T00:01:00Z' }
    ]);
    await second;
    oldRequest.flush([{ id: 'old', senderId: 'person-1', text: 'Tin cũ', createdAt: '2026-11-10T00:00:00Z' }]);
    await first;
    expect(app.messages().map(message => message.id)).toEqual(['new']);
    app.closeChat();
    fixture.destroy();
    http.verify();
  });
});
