import { Component, OnInit, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MessagingService, ConversationSummary } from '@core/services/messaging.service';

@Component({
  selector: 'lll-inbox',
  standalone: true,
  imports: [CommonModule, RouterLink, DatePipe],
  templateUrl: './inbox.component.html'
})
export class InboxComponent implements OnInit {
  conversations = signal<ConversationSummary[]>([]);
  loading       = signal(true);

  constructor(private messaging: MessagingService) {}

  ngOnInit(): void {
    this.messaging.getConversations().subscribe({
      next: (c) => { this.conversations.set(c); this.loading.set(false); },
      error: ()  => this.loading.set(false)
    });
  }
}
