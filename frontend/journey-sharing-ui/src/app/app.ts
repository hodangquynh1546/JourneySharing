import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, JourneyPost, Connection, ChatMessage } from './api.service';
import { LocationPicker, LocationPoint } from './location-picker';

type View = 'discover' | 'account' | 'my-posts' | 'connections' | 'create';
type PostKind = 'trip' | 'request';

@Component({
  selector: 'app-root',
  imports: [CommonModule, FormsModule, LocationPicker],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit, OnDestroy {
  readonly api = inject(ApiService);
  readonly view = signal<View>('discover');
  readonly trips = signal<JourneyPost[]>([]);
  readonly requests = signal<JourneyPost[]>([]);
  readonly myTrips = signal<JourneyPost[]>([]);
  readonly myRequests = signal<JourneyPost[]>([]);
  readonly connections = signal<Connection[]>([]);
  readonly activeChat = signal<Connection | null>(null);
  readonly messages = signal<ChatMessage[]>([]);
  readonly busy = signal(false);
  readonly notice = signal('');
  readonly error = signal('');
  readonly registerMode = signal(false);
  readonly vehicles = signal<{ id: string; type: string; model?: string; shareableSeats: number }[]>([]);
  readonly postKind = signal<PostKind>('trip');
  readonly selectedPost = signal<JourneyPost | null>(null);
  readonly selectedKind = signal<'request' | 'trip'>('request');
  readonly selectedDriverPost = signal<JourneyPost | null>(null);
  readonly nearbySearchOpen = signal(false);
  search = { fromArea: '', toArea: '', date: '' };
  searchPoint = { fromLat: null as number | null, fromLng: null as number | null,
    toLat: null as number | null, toLng: null as number | null };
  radiusKm = 10;
  account = { fullName: '', email: '', password: '' };
  vehicle = { type: '', model: '', color: '', shareableSeats: 1 };
  post = this.emptyPost();
  selectedDriverId = '';
  selectedPassengerId = '';
  messageDraft = '';
  private chatRefreshTimer: number | null = null;
  private pendingChatId: string | null = null;

  ngOnInit(): void { void this.loadPublic(); void this.api.restoreSession(); }

  ngOnDestroy(): void { this.stopChatRefresh(); }

  private async perform(action: () => Promise<void>, success = ''): Promise<void> {
    this.busy.set(true);
    this.error.set('');
    this.notice.set('');
    try {
      await action();
      if (success) this.notice.set(success);
    } catch (error: unknown) {
      const e = error as { status?: number; error?: { error?: string } };
      this.error.set(e.error?.error || (error instanceof Error && !('status' in error) ? error.message : '') || (e.status === 0
        ? 'Không kết nối được máy chủ. Kiểm tra mạng và địa chỉ API.'
        : e.status === 401 ? 'Thông tin đăng nhập không đúng hoặc phiên đã hết hạn.'
        : 'Không thực hiện được. Vui lòng kiểm tra dữ liệu và thử lại.'));
    } finally {
      this.busy.set(false);
    }
  }

  async loadPublic(): Promise<void> {
    const params = Object.fromEntries(Object.entries(this.search).filter(([, v]) => v.trim()));
    if (this.searchPoint.fromLat !== null && this.searchPoint.fromLng !== null) {
      params['fromLat'] = String(this.searchPoint.fromLat);
      params['fromLng'] = String(this.searchPoint.fromLng);
    }
    if (this.searchPoint.toLat !== null && this.searchPoint.toLng !== null) {
      params['toLat'] = String(this.searchPoint.toLat);
      params['toLng'] = String(this.searchPoint.toLng);
    }
    if (this.searchPoint.fromLat !== null || this.searchPoint.toLat !== null)
      params['radiusKm'] = String(this.radiusKm);
    await this.perform(async () => {
      const [trips, requests] = await Promise.all([this.api.trips(params), this.api.requests(params)]);
      this.trips.set(trips);
      this.requests.set(requests);
    });
  }

  searchPicked(event: { target: 'origin' | 'destination'; point: LocationPoint }): void {
    if (event.target === 'origin') {
      this.searchPoint.fromLat = event.point.latitude;
      this.searchPoint.fromLng = event.point.longitude;
    } else {
      this.searchPoint.toLat = event.point.latitude;
      this.searchPoint.toLng = event.point.longitude;
    }
  }

  clearSearchPoints(): void {
    this.searchPoint = { fromLat: null, fromLng: null, toLat: null, toLng: null };
    void this.loadPublic();
  }

  async authenticate(): Promise<void> {
    await this.perform(async () => {
      if (this.registerMode())
        await this.api.register(this.account.fullName, this.account.email, this.account.password);
      else await this.api.login(this.account.email, this.account.password);
      this.account.password = '';
      this.view.set('discover');
    }, 'Đăng nhập thành công.');
  }

  async open(view: View): Promise<void> {
    if (view !== 'connections') this.closeChat();
    this.view.set(view);
    if (!this.api.profile()) return;
    if (view === 'my-posts') await this.perform(async () => {
      const [trips, requests] = await Promise.all([this.api.myTrips(), this.api.myRequests()]);
      this.myTrips.set(trips);
      this.myRequests.set(requests);
    });
    if (view === 'connections') await this.perform(async () => {
      this.connections.set(await this.api.connections());
    });
    if (view === 'create') await this.perform(async () => {
      this.vehicles.set(await this.api.myVehicles());
    });
  }

  private emptyPost() {
    return {
      originArea: '', destinationArea: '', originAddress: '', destinationAddress: '',
      originLatitude: null as number | null, originLongitude: null as number | null,
      destinationLatitude: null as number | null, destinationLongitude: null as number | null,
      departureTime: '', expiresAt: '', passengerCount: 1, vehicleId: '', availableSeats: 1,
      contributionAmount: null as number | null
    };
  }

  picked(event: { target: 'origin' | 'destination'; point: LocationPoint }): void {
    if (event.target === 'origin') {
      this.post.originLatitude = event.point.latitude;
      this.post.originLongitude = event.point.longitude;
    } else {
      this.post.destinationLatitude = event.point.latitude;
      this.post.destinationLongitude = event.point.longitude;
    }
  }

  async addVehicle(): Promise<void> {
    await this.perform(async () => {
      const created = await this.api.createVehicle(this.vehicle);
      this.vehicles.set([...this.vehicles(), created]);
      this.post.vehicleId = created.id;
      this.vehicle = { type: '', model: '', color: '', shareableSeats: 1 };
    }, 'Đã thêm phương tiện.');
  }

  async publishPost(): Promise<void> {
    const p = this.post;
    if (p.originLatitude === null || p.originLongitude === null ||
        p.destinationLatitude === null || p.destinationLongitude === null) {
      this.error.set('Hãy chọn đủ điểm đi và điểm đến trên bản đồ.'); return;
    }
    if (!p.departureTime || !p.expiresAt || new Date(p.departureTime) <= new Date() ||
        new Date(p.expiresAt) > new Date(p.departureTime) || new Date(p.expiresAt) <= new Date()) {
      this.error.set('Giờ đi phải ở tương lai; thời điểm hết hạn phải ở trước hoặc bằng giờ đi.'); return;
    }
    const origin = { latitude: p.originLatitude, longitude: p.originLongitude, address: p.originAddress.trim() };
    const destination = { latitude: p.destinationLatitude, longitude: p.destinationLongitude, address: p.destinationAddress.trim() };
    const shared = {
      origin, destination, originArea: p.originArea.trim(), destinationArea: p.destinationArea.trim(),
      expiresAt: new Date(p.expiresAt).toISOString(), passengerCount: Number(p.passengerCount)
    };
    await this.perform(async () => {
      if (this.postKind() === 'trip') {
        await this.api.createTrip({ ...shared, departureTime: new Date(p.departureTime).toISOString(),
          vehicleId: p.vehicleId || null, availableSeats: p.vehicleId ? Number(p.availableSeats) : 0,
          contributionAmount: p.contributionAmount });
      } else {
        await this.api.createRequest({ ...shared, desiredTime: new Date(p.departureTime).toISOString(),
          proposedContribution: p.contributionAmount, preferredVehicleType: null });
      }
      this.post = this.emptyPost();
      const [publicTrips, publicRequests, myTrips, myRequests] = await Promise.all([
        this.api.trips(), this.api.requests(), this.api.myTrips(), this.api.myRequests()
      ]);
      this.trips.set(publicTrips); this.requests.set(publicRequests);
      this.myTrips.set(myTrips); this.myRequests.set(myRequests);
      this.view.set('my-posts');
    }, 'Đã đăng bài.');
  }

  async cancelPost(kind: PostKind, id: string): Promise<void> {
    if (!window.confirm('Bạn muốn hủy bài đăng này?')) return;
    await this.perform(async () => {
      await this.api.cancelPost(kind, id);
      const [myTrips, myRequests, publicTrips, publicRequests] = await Promise.all([
        this.api.myTrips(), this.api.myRequests(), this.api.trips(), this.api.requests()
      ]);
      this.myTrips.set(myTrips); this.myRequests.set(myRequests);
      this.trips.set(publicTrips); this.requests.set(publicRequests);
    }, 'Đã hủy bài đăng.');
  }

  async chooseConnection(post: JourneyPost, kind: 'request' | 'trip'): Promise<void> {
    if (!this.api.profile()) { this.view.set('account'); return; }
    await this.perform(async () => {
      this.myTrips.set(await this.api.myTrips());
      this.selectedPost.set(post);
      this.selectedKind.set(kind);
      this.selectedDriverId = '';
    });
  }

  async chooseDriverConnection(post: JourneyPost): Promise<void> {
    if (!this.api.profile()) { this.view.set('account'); return; }
    await this.perform(async () => {
      const [requests, trips] = await Promise.all([this.api.myRequests(), this.api.myTrips()]);
      this.myRequests.set(requests); this.myTrips.set(trips);
      this.selectedDriverPost.set(post);
      this.selectedPassengerId = '';
    });
  }

  async sendDriverConnection(): Promise<void> {
    const driver = this.selectedDriverPost();
    if (!driver || !this.selectedPassengerId) return;
    await this.perform(async () => {
      await this.api.connect(driver.id, this.selectedPassengerId.slice(2),
        this.selectedPassengerId.startsWith('r:') ? 'request' : 'trip');
      this.selectedDriverPost.set(null);
      this.connections.set(await this.api.connections());
      this.view.set('connections');
    }, 'Đã gửi lời mời kết nối.');
  }

  async sendConnection(): Promise<void> {
    const selected = this.selectedPost();
    if (!selected || !this.selectedDriverId) return;
    await this.perform(async () => {
      await this.api.connect(this.selectedDriverId, selected.id, this.selectedKind());
      this.selectedPost.set(null);
      this.connections.set(await this.api.connections());
      this.view.set('connections');
    }, 'Đã gửi lời mời kết nối.');
  }

  statusText(status?: string): string {
    return ({ Open: 'Đang mở', Pending: 'Đang chờ', Accepted: 'Đã chấp nhận',
      Rejected: 'Đã từ chối', Cancelled: 'Đã hủy', Expired: 'Hết hạn',
      InProgress: 'Đang đi', Completed: 'Hoàn tất' } as Record<string, string>)[status || ''] || status || '';
  }

  async act(connection: Connection, action: 'accept' | 'reject' | 'cancel'): Promise<void> {
    await this.perform(async () => {
      await this.api.actOnConnection(connection.id, action);
      const updated = await this.api.connections();
      this.connections.set(updated);
      if (this.activeChat()?.id === connection.id)
        this.activeChat.set(updated.find(item => item.id === connection.id) || null);
    }, 'Đã cập nhật yêu cầu kết nối.');
  }

  async cancelAccepted(connection: Connection): Promise<void> {
    if (!window.confirm('Hủy kết nối đã chấp nhận? Chỗ sẽ được trả lại và cả hai bên có thể xem lịch sử chat.')) return;
    await this.act(connection, 'cancel');
  }

  async openChat(connection: Connection): Promise<void> {
    this.stopChatRefresh();
    this.activeChat.set(connection);
    this.messages.set([]);
    await this.refreshChat();
    if (this.activeChat()?.id === connection.id) {
      this.chatRefreshTimer = window.setInterval(() => {
        if (document.visibilityState === 'visible') void this.refreshChat(true);
      }, 5000);
    }
  }

  async refreshChat(silent = false): Promise<void> {
    const connection = this.activeChat();
    if (!connection || this.pendingChatId === connection.id) return;
    this.pendingChatId = connection.id;
    try {
      if (silent) {
        const messages = await this.api.messages(connection.id);
        if (this.activeChat()?.id === connection.id) this.mergeChatMessages(messages);
      } else {
        await this.perform(async () => {
          const messages = await this.api.messages(connection.id);
          if (this.activeChat()?.id === connection.id) this.mergeChatMessages(messages);
        });
      }
    } catch { /* The next refresh retries when the connection is available. */ }
    finally { if (this.pendingChatId === connection.id) this.pendingChatId = null; }
  }

  private mergeChatMessages(received: ChatMessage[]): void {
    const receivedIds = new Set(received.map(message => message.id));
    const newest = [...received, ...this.messages().filter(message => !receivedIds.has(message.id))];
    newest.sort((a, b) => a.createdAt.localeCompare(b.createdAt) || a.id.localeCompare(b.id));
    this.messages.set(newest);
  }

  closeChat(): void {
    this.stopChatRefresh();
    this.activeChat.set(null);
    this.messages.set([]);
  }

  private stopChatRefresh(): void {
    if (this.chatRefreshTimer !== null) window.clearInterval(this.chatRefreshTimer);
    this.chatRefreshTimer = null;
  }

  async sendChatMessage(): Promise<void> {
    const connection = this.activeChat();
    const text = this.messageDraft.trim();
    if (!connection || !text) return;
    await this.perform(async () => {
      const sent = await this.api.sendMessage(connection.id, text);
      this.messages.set([...this.messages(), sent]);
      this.messageDraft = '';
    });
  }

  signOut(): void {
    this.closeChat();
    this.api.logout();
    this.myTrips.set([]);
    this.myRequests.set([]);
    this.connections.set([]);
    this.vehicles.set([]);
    this.view.set('discover');
  }
}
