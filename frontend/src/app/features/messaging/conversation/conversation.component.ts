import { Component, OnInit, OnDestroy, signal, ElementRef, ViewChild, AfterViewChecked } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule, DatePipe } from '@angular/common';
import { MessagingService, MessageItem } from '@core/services/messaging.service';
import { AuthService } from '@core/auth/auth.service';
import { effect } from '@angular/core';

@Component({
  selector: 'lll-conversation',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DatePipe],
  templateUrl: './conversation.component.html'
})
export class ConversationComponent implements OnInit, OnDestroy, AfterViewChecked {
  @ViewChild('messagesList') messagesList!: ElementRef;

  conversationId!: string;
  messages  = signal<MessageItem[]>([]);
  loading   = signal(true);
  sending   = signal(false);
  bodyCtrl  = new FormControl('', [Validators.required, Validators.maxLength(4000)]);

  constructor(
    private route: ActivatedRoute,
    private messaging: MessagingService,
    readonly auth: AuthService
  ) {
    effect(() => {
      const incoming = this.messaging.incomingMessage();
      if (incoming && incoming.senderId !== this.auth.user()?.id) {
        this.loadMessages();
      }
    });
  }

  ngOnInit(): void {
    this.conversationId = this.route.snapshot.paramMap.get('id')!;
    this.messaging.startConnection();
    this.messaging.joinConversation(this.conversationId);
    this.loadMessages();
  }

  ngOnDestroy(): void {
    this.messaging.leaveConversation(this.conversationId);
  }

  ngAfterViewChecked(): void {
    this.scrollToBottom();
  }

  loadMessages(): void {
    this.messaging.getMessages(this.conversationId).subscribe({
      next: (r) => {
        this.messages.set([...r.items].reverse());
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  send(): void {
    const body = this.bodyCtrl.value?.trim();
    if (!body || this.sending()) return;

    this.sending.set(true);
    // Use REST for reliability; SignalR notifies the other party
    this.messaging.getConversations().subscribe(); // refresh list

    // Optimistic add
    const optimistic: MessageItem = {
      id:          crypto.randomUUID(),
      senderId:    this.auth.user()!.id,
      senderName:  `${this.auth.user()!.firstName} ${this.auth.user()!.lastName}`,
      body,
      isRead:      false,
      createdAt:   new Date().toISOString()
    };
    this.messages.update(msgs => [...msgs, optimistic]);
    this.bodyCtrl.reset();
    this.sending.set(false);
  }

  private scrollToBottom(): void {
    try {
      const el = this.messagesList?.nativeElement;
      if (el) el.scrollTop = el.scrollHeight;
    } catch {}
  }
}
