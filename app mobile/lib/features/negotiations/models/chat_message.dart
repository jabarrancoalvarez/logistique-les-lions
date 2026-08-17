/// Mensaje del chat (`MessageDto`). También se construye desde el push de SignalR.
class ChatMessage {
  final String id;
  final String senderId;
  final String senderName;
  final String? senderAvatar;
  final String body;
  final bool isRead;
  final DateTime createdAt;

  const ChatMessage({
    required this.id,
    required this.senderId,
    required this.body,
    required this.createdAt,
    this.senderName = '',
    this.senderAvatar,
    this.isRead = false,
  });

  factory ChatMessage.fromJson(Map<String, dynamic> j) => ChatMessage(
        id: j['id'] as String,
        senderId: (j['senderId'] ?? '') as String,
        senderName: (j['senderName'] ?? '') as String,
        senderAvatar: j['senderAvatar'] as String?,
        body: (j['body'] ?? '') as String,
        isRead: j['isRead'] as bool? ?? false,
        createdAt:
            DateTime.tryParse((j['createdAt'] ?? '') as String) ?? DateTime.now(),
      );

  /// Desde el evento `ReceiveMessage` del hub (claves PascalCase del servidor).
  factory ChatMessage.fromHub(Map<String, dynamic> j) => ChatMessage(
        id: (j['messageId'] ?? j['MessageId'] ?? '') as String,
        senderId: (j['senderId'] ?? j['SenderId'] ?? '') as String,
        body: (j['body'] ?? j['Body'] ?? '') as String,
        createdAt: DateTime.tryParse(
                (j['createdAt'] ?? j['CreatedAt'] ?? '') as String) ??
            DateTime.now(),
      );
}
