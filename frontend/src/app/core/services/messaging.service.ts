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

@Injectable({ providedIn: 'root' })
export class MessagingService {
  private readonly apiUrl = `${environment.apiUrl}/v1/messaging`;
  private hubConnection?: signalR.HubConnection;

  readonly incomingMessage = signal<IncomingMessage | null>(null);
  readonly isConnected     = signal(false);

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

  startConnection(): void {
    const token = this.auth.accessToken();
    if (!token || this.isConnected()) return;

    const hubUrl = environment.apiUrl.replace('/api', '') + '/hubs/chat';

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.hubConnection.on('ReceiveMessage', (msg: IncomingMessage) => {
      this.incomingMessage.set(msg);
    });

    this.hubConnection
      .start()
      .then(() => this.isConnected.set(true))
      .catch(err => console.error('SignalR error:', err));
  }

  stopConnection(): void {
    this.hubConnection?.stop().then(() => this.isConnected.set(false));
  }

  sendMessageHub(recipientId: string, vehicleId: string, body: string): void {
    this.hubConnection?.invoke('SendMessage', recipientId, vehicleId, body);
  }

  joinConversation(conversationId: string): void {
    this.hubConnection?.invoke('JoinConversation', conversationId);
  }

  leaveConversation(conversationId: string): void {
    this.hubConnection?.invoke('LeaveConversation', conversationId);
  }
}
