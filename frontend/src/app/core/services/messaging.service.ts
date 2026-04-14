import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '@environments/environment';
import * as signalR from '@microsoft/signalr';
import { AuthService } from '@core/auth/auth.service';

export interface ConversationSummary {
  id: string;
  otherUserId: string;
  otherUserName: string;
  otherUserAvatar?: string;
  vehicleId: string;
  vehicleTitle: string;
  vehicleThumbnail?: string;
  lastMessage?: string;
  lastMessageAt?: string;
  unreadCount: number;
}

export interface MessageItem {
  id: string;
  senderId: string;
  senderName: string;
  senderAvatar?: string;
  body: string;
  isRead: boolean;
  createdAt: string;
}

export interface IncomingMessage {
  messageId: string;
  senderId: string;
  vehicleId: string;
  body: string;
  createdAt: string;
}

export interface TypingNotification {
  senderId: string;
  vehicleId: string;
}

export interface ReadReceipt {
  readerId: string;
  vehicleId: string;
  readAt: string;
}

@Injectable({ providedIn: 'root' })
export class MessagingService {
  private readonly apiUrl = `${environment.apiUrl}/v1/messaging`;
  private hubConnection?: signalR.HubConnection;

  readonly incomingMessage = signal<IncomingMessage | null>(null);
  readonly typingNotification = signal<TypingNotification | null>(null);
  readonly readReceipt        = signal<ReadReceipt | null>(null);
  readonly isConnected     = signal(false);

  private startPromise?: Promise<void>;

  constructor(private http: HttpClient, private auth: AuthService) {}

  // ─── REST ────────────────────────────────────────────────────────────────

  getConversations(): Observable<ConversationSummary[]> {
    return this.http.get<{ isSuccess: boolean; value: ConversationSummary[] }>(`${this.apiUrl}/conversations`).pipe(
      map(r => r.value)
    );
  }

  getMessages(conversationId: string, page = 1): Observable<{ items: MessageItem[]; totalCount: number }> {
    return this.http.get<{ isSuccess: boolean; value: { items: MessageItem[]; totalCount: number } }>(
      `${this.apiUrl}/conversations/${conversationId}/messages?page=${page}&pageSize=50`
    ).pipe(map(r => r.value));
  }

  sendMessageRest(recipientId: string, vehicleId: string, body: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/send`, { recipientId, vehicleId, body });
  }

  // ─── SignalR ──────────────────────────────────────────────────────────────

  startConnection(): Promise<void> {
    if (this.startPromise) return this.startPromise;

    const token = this.auth.accessToken();
    if (!token) return Promise.resolve();

    const hubUrl = environment.apiUrl.replace('/api', '') + '/hubs/chat';

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.hubConnection.on('ReceiveMessage', (msg: IncomingMessage) => {
      this.incomingMessage.set(msg);
    });

    this.hubConnection.on('UserTyping', (n: TypingNotification) => {
      this.typingNotification.set(n);
    });

    this.hubConnection.on('MessageRead', (r: ReadReceipt) => {
      this.readReceipt.set(r);
    });

    this.startPromise = this.hubConnection
      .start()
      .then(() => { this.isConnected.set(true); })
      .catch(err => {
        console.error('SignalR error:', err);
        this.startPromise = undefined;
      });

    return this.startPromise;
  }

  private async ensureConnected(): Promise<boolean> {
    if (!this.hubConnection) await this.startConnection();
    if (this.startPromise) await this.startPromise;
    return this.hubConnection?.state === signalR.HubConnectionState.Connected;
  }

  stopConnection(): void {
    this.hubConnection?.stop().then(() => {
      this.isConnected.set(false);
      this.startPromise = undefined;
    });
  }

  async sendMessageHub(recipientId: string, vehicleId: string, body: string): Promise<void> {
    if (await this.ensureConnected()) {
      await this.hubConnection!.invoke('SendMessage', recipientId, vehicleId, body);
    }
  }

  async joinConversation(conversationId: string): Promise<void> {
    if (await this.ensureConnected()) {
      await this.hubConnection!.invoke('JoinConversation', conversationId);
    }
  }

  async leaveConversation(conversationId: string): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      await this.hubConnection.invoke('LeaveConversation', conversationId);
    }
  }

  async startTyping(recipientId: string, vehicleId: string): Promise<void> {
    if (await this.ensureConnected()) {
      await this.hubConnection!.invoke('StartTyping', recipientId, vehicleId);
    }
  }

  async markAsRead(senderId: string, vehicleId: string): Promise<void> {
    if (await this.ensureConnected()) {
      await this.hubConnection!.invoke('MarkAsRead', senderId, vehicleId);
    }
  }
}
