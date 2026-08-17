import 'package:dio/dio.dart';
import '../../../core/network/api_client.dart';
import '../models/chat_message.dart';
import '../models/contract.dart';
import '../models/inspection.dart';
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

  // ─── Inspection ─────────────────────────────────────────────────────────

  /// `GET /negotiations/{id}/inspection` (valor directo).
  Future<Inspection> getInspection(String negotiationId) async {
    final res = await _api.dio.get('/negotiations/$negotiationId/inspection');
    return Inspection.fromJson(res.data as Map<String, dynamic>);
  }

  /// `PUT /negotiations/{id}/inspection`.
  Future<void> saveInspection(
    String negotiationId, {
    DateTime? visitedAt,
    int? observedMileage,
    String? notes,
    required List<InspectionItem> items,
  }) async {
    await _api.dio.put('/negotiations/$negotiationId/inspection', data: {
      'visitedAt': visitedAt?.toUtc().toIso8601String(),
      'observedMileage': observedMileage,
      'notes': notes,
      'items': items.map((e) => e.toJson()).toList(),
    });
  }

  // ─── Contrat ────────────────────────────────────────────────────────────

  /// `GET /negotiations/{id}/contract` (valor directo).
  Future<ContractTab> getContractTab(String negotiationId) async {
    final res = await _api.dio.get('/negotiations/$negotiationId/contract');
    return ContractTab.fromJson(res.data as Map<String, dynamic>);
  }

  /// `POST /negotiations/{id}/contract` — crea el contrato. Devuelve su id.
  Future<String> createContract(
      String negotiationId, Map<String, dynamic> body) async {
    final res =
        await _api.dio.post('/negotiations/$negotiationId/contract', data: body);
    return (res.data as Map<String, dynamic>)['id'] as String;
  }

  /// `PUT /negotiations/contracts/{id}` — corrige un borrador.
  Future<void> updateContract(
      String contractId, Map<String, dynamic> body) async {
    await _api.dio.put('/negotiations/contracts/$contractId', data: body);
  }

  Future<void> sendContract(String contractId) =>
      _api.dio.post('/negotiations/contracts/$contractId/send');

  Future<void> validateContract(String contractId) =>
      _api.dio.post('/negotiations/contracts/$contractId/validate');

  Future<void> requestContractChanges(String contractId, String notes) =>
      _api.dio.post('/negotiations/contracts/$contractId/request-changes',
          data: {'notes': notes});

  Future<void> cancelContract(String contractId) =>
      _api.dio.post('/negotiations/contracts/$contractId/cancel');

  /// Descarga el PDF del contrato validado (con el JWT del interceptor).
  Future<List<int>> downloadContractPdf(String contractId) async {
    final res = await _api.dio.get<List<int>>(
      '/negotiations/contracts/$contractId.pdf',
      options: Options(responseType: ResponseType.bytes),
    );
    return res.data ?? const [];
  }
}
