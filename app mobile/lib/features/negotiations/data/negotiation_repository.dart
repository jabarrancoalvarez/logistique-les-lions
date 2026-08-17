import '../../../core/network/api_client.dart';
import '../models/chat_message.dart';
import '../models/negotiation_detail.dart';
import '../models/negotiation_summary.dart';

/// Acceso a «Mes négociations» y a la mensajería (mismos endpoints que la web).
///
/// Ojo con el envoltorio: los endpoints de `/negotiations` devuelven el valor
/// directo, mientras que los de `/messaging` van envueltos en `Result<T>` (`value`).
class NegotiationRepository {
  NegotiationRepository(this._api);

  final ApiClient _api;

  /// `GET /negotiations?status=`. `status`: EnCours | EnAttente | Terminee.
  Future<List<NegotiationSummary>> getMyNegotiations({String? status}) async {
    final res = await _api.dio.get('/negotiations', queryParameters: {
      'status': ?status,
    });
    return (res.data as List<dynamic>)
        .map((e) => NegotiationSummary.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  /// `GET /negotiations/{id}`.
  Future<NegotiationDetail> getNegotiation(String id) async {
    final res = await _api.dio.get('/negotiations/$id');
    return NegotiationDetail.fromJson(res.data as Map<String, dynamic>);
  }

  /// `GET /messaging/conversations/{negotiationId}/messages` (envuelto en Result).
  Future<List<ChatMessage>> getMessages(String negotiationId,
      {int page = 1, int pageSize = 50}) async {
    final res = await _api.dio.get(
      '/messaging/conversations/$negotiationId/messages',
      queryParameters: {'page': page, 'pageSize': pageSize},
    );
    final value = (res.data as Map<String, dynamic>)['value']
        as Map<String, dynamic>?;
    final items = value?['items'] as List<dynamic>? ?? const [];
    return items
        .map((e) => ChatMessage.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  /// `POST /messaging/send`. Crea la negociación si no existía. Devuelve el id
  /// del mensaje.
  Future<String> sendMessage({
    required String recipientId,
    required String vehicleId,
    required String body,
  }) async {
    final res = await _api.dio.post('/messaging/send', data: {
      'recipientId': recipientId,
      'vehicleId': vehicleId,
      'body': body,
    });
    return (res.data as Map<String, dynamic>)['value'] as String;
  }

  /// `POST /negotiations/offers`. Devuelve (negotiationId, offerId?).
  Future<({String negotiationId, String? offerId})> makeOffer({
    required String vehicleId,
    num? amount,
    String? message,
  }) async {
    final res = await _api.dio.post('/negotiations/offers', data: {
      'vehicleId': vehicleId,
      'amount': amount,
      'message': message,
    });
    final data = res.data as Map<String, dynamic>;
    return (
      negotiationId: data['negotiationId'] as String,
      offerId: data['offerId'] as String?,
    );
  }

  /// `POST /negotiations/{id}/offers` — contraoferta.
  Future<void> counterOffer(String negotiationId,
      {required num amount, String? message}) async {
    await _api.dio.post('/negotiations/$negotiationId/offers', data: {
      'amount': amount,
      'message': message,
    });
  }

  /// `POST /negotiations/offers/{offerId}/accept`.
  Future<void> acceptOffer(String offerId) async {
    await _api.dio.post('/negotiations/offers/$offerId/accept');
  }

  /// `POST /negotiations/offers/{offerId}/reject`.
  Future<void> rejectOffer(String offerId) async {
    await _api.dio.post('/negotiations/offers/$offerId/reject');
  }
}
