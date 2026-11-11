import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { Capacitor } from '@capacitor/core';

export interface JourneyPost {
  id: string;
  originArea: string;
  destinationArea: string;
  departureTime?: string;
  desiredTime?: string;
  remainingSeats?: number;
  passengerCount: number;
  hasVehicle?: boolean;
  vehicleId?: string | null;
  contributionAmount?: number;
  proposedContribution?: number;
  status?: string;
}

export interface Vehicle { id: string; type: string; model?: string; color?: string; shareableSeats: number; }

export interface Connection {
  id: string;
  tripId: string;
  travelRequestId?: string;
  otherTripId?: string;
  senderId: string;
  status: string;
  reservedSeats: number;
  acceptedAt?: string | null;
}

export interface ChatMessage { id: string; senderId: string; text: string; createdAt: string; }

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly base =
    (window as Window & { JOURNEY_API_URL?: string }).JOURNEY_API_URL ||
    (Capacitor.isNativePlatform() ? '' :
      ['localhost', '127.0.0.1'].includes(location.hostname) ? 'http://localhost:8080' : location.origin);
  readonly token = signal('');
  readonly profile = signal<{ id: string; fullName: string; email: string } | null>(null);
  readonly sessionEpoch = signal(0);
  private expiryTimer: number | null = null;

  private armSessionExpiry(expiresAt: string): void {
    if (this.expiryTimer !== null) window.clearTimeout(this.expiryTimer);
    const remaining = new Date(expiresAt).getTime() - Date.now();
    if (!Number.isFinite(remaining) || remaining <= 0) { this.logout(); return; }
    this.expiryTimer = window.setTimeout(() => this.armSessionExpiry(expiresAt),
      Math.min(remaining, 2147483647));
  }

  private async authorized<T>(request: Promise<T>, usedToken = this.token()): Promise<T> {
    try { return await request; }
    catch (error) {
      if ((error as { status?: number }).status === 401 && this.token() === usedToken) this.logout();
      throw error;
    }
  }

  private headers(): { headers: HttpHeaders } {
    return { headers: new HttpHeaders({ Authorization: `Bearer ${this.token()}` }) };
  }

  private url(path: string): string {
    if (!this.base) throw new Error('Chưa cấu hình địa chỉ API cho website.');
    return `${this.base}${path}`;
  }

  async login(email: string, password: string): Promise<void> {
    const response = await firstValueFrom(this.http.post<{ accessToken: string; expiresAt: string }>(
      this.url(`/api/v1/auth/login`), { email, password }));
    this.token.set(response.accessToken);
    try {
      this.profile.set(await firstValueFrom(this.http.get<{ id: string; fullName: string; email: string }>(
        this.url(`/api/v1/users/me`), this.headers())));
      sessionStorage.setItem('journey-session', JSON.stringify(response));
      this.armSessionExpiry(response.expiresAt);
    } catch (error) {
      this.logout();
      throw error;
    }
  }

  async restoreSession(): Promise<void> {
    const saved = sessionStorage.getItem('journey-session');
    if (!saved) return;
    try {
      const session = JSON.parse(saved) as { accessToken: string; expiresAt: string };
      if (!session.accessToken || new Date(session.expiresAt) <= new Date()) { this.logout(); return; }
      this.token.set(session.accessToken);
      this.profile.set(await firstValueFrom(this.http.get<{ id: string; fullName: string; email: string }>(
        this.url('/api/v1/users/me'), this.headers())));
      this.armSessionExpiry(session.expiresAt);
    } catch { this.logout(); }
  }

  async register(fullName: string, email: string, password: string): Promise<void> {
    await firstValueFrom(this.http.post(this.url(`/api/v1/auth/register`), { fullName, email, password }));
    await this.login(email, password);
  }

  logout(): void {
    if (this.expiryTimer !== null) window.clearTimeout(this.expiryTimer);
    this.expiryTimer = null;
    sessionStorage.removeItem('journey-session');
    this.token.set('');
    this.profile.set(null);
    this.sessionEpoch.update(value => value + 1);
  }

  trips(params: Record<string, string> = {}): Promise<JourneyPost[]> {
    return firstValueFrom(this.http.get<JourneyPost[]>(this.url(`/api/v1/trips`), { params }));
  }

  requests(params: Record<string, string> = {}): Promise<JourneyPost[]> {
    return firstValueFrom(this.http.get<JourneyPost[]>(this.url(`/api/v1/travel-requests`), { params }));
  }

  myTrips(): Promise<JourneyPost[]> {
    return this.authorized(firstValueFrom(this.http.get<JourneyPost[]>(this.url(`/api/v1/trips/me`), this.headers())));
  }

  myRequests(): Promise<JourneyPost[]> {
    return this.authorized(firstValueFrom(this.http.get<JourneyPost[]>(this.url(`/api/v1/travel-requests/me`), this.headers())));
  }

  myVehicles(): Promise<Vehicle[]> {
    return this.authorized(firstValueFrom(this.http.get<Vehicle[]>(this.url('/api/v1/vehicles/me'), this.headers())));
  }

  createVehicle(input: { type: string; model?: string; color?: string; shareableSeats: number }): Promise<Vehicle> {
    return this.authorized(firstValueFrom(this.http.post<Vehicle>(this.url('/api/v1/vehicles'), input, this.headers())));
  }

  cancelPost(kind: 'trip' | 'request', id: string): Promise<unknown> {
    const path = kind === 'trip' ? 'trips' : 'travel-requests';
    return this.authorized(firstValueFrom(this.http.post(this.url(`/api/v1/${path}/${id}/cancel`), {}, this.headers())));
  }

  createTrip(input: unknown): Promise<JourneyPost> {
    return this.authorized(firstValueFrom(this.http.post<JourneyPost>(this.url('/api/v1/trips'), input, this.headers())));
  }

  createRequest(input: unknown): Promise<JourneyPost> {
    return this.authorized(firstValueFrom(this.http.post<JourneyPost>(this.url('/api/v1/travel-requests'), input, this.headers())));
  }

  connections(): Promise<Connection[]> {
    return this.authorized(firstValueFrom(this.http.get<Connection[]>(this.url(`/api/v1/connections`), this.headers())));
  }

  connect(tripId: string, counterpartId: string, kind: 'request' | 'trip'): Promise<unknown> {
    return this.authorized(firstValueFrom(this.http.post(this.url(`/api/v1/connections`), {
      tripId, travelRequestId: kind === 'request' ? counterpartId : null,
      otherTripId: kind === 'trip' ? counterpartId : null
    }, this.headers())));
  }

  actOnConnection(id: string, action: 'accept' | 'reject' | 'cancel'): Promise<unknown> {
    return this.authorized(firstValueFrom(this.http.post(this.url(`/api/v1/connections/${id}/${action}`),
      {}, this.headers())));
  }

  messages(connectionId: string): Promise<ChatMessage[]> {
    return this.authorized(firstValueFrom(this.http.get<ChatMessage[]>(
      this.url(`/api/v1/connections/${connectionId}/messages`), this.headers())));
  }

  sendMessage(connectionId: string, text: string): Promise<ChatMessage> {
    return this.authorized(firstValueFrom(this.http.post<ChatMessage>(
      this.url(`/api/v1/connections/${connectionId}/messages`), { text }, this.headers())));
  }
}
