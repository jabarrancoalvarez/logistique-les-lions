class Valuation {
  final bool hasEstimate;
  final num? estimatedValue;
  final num? lowValue;
  final num? highValue;
  final int comparableCount;
  final ValuationEvolution? evolution;

  const Valuation({
    required this.hasEstimate,
    required this.comparableCount,
    this.estimatedValue,
    this.lowValue,
    this.highValue,
    this.evolution,
  });

  factory Valuation.fromJson(Map<String, dynamic> j) => Valuation(
        hasEstimate: j['hasEstimate'] as bool? ?? false,
        estimatedValue: j['estimatedValue'] as num?,
        lowValue: j['lowValue'] as num?,
        highValue: j['highValue'] as num?,
        comparableCount: j['comparableCount'] as int? ?? 0,
        evolution: j['evolution'] == null
            ? null
            : ValuationEvolution.fromJson(j['evolution'] as Map<String, dynamic>),
      );
}

class ValuationEvolution {
  final int monthsCovered;
  final num? changeAmount;
  final num? changePercent;

  const ValuationEvolution({
    required this.monthsCovered,
    this.changeAmount,
    this.changePercent,
  });

  factory ValuationEvolution.fromJson(Map<String, dynamic> j) =>
      ValuationEvolution(
        monthsCovered: j['monthsCovered'] as int? ?? 0,
        changeAmount: j['changeAmount'] as num?,
        changePercent: j['changePercent'] as num?,
      );
}

class Completeness {
  final int score;
  final String level;
  final List<CompletenessItem> items;

  const Completeness(
      {required this.score, required this.level, required this.items});

  factory Completeness.fromJson(Map<String, dynamic> j) => Completeness(
        score: j['score'] as int? ?? 0,
        level: (j['level'] ?? '') as String,
        items: (j['items'] as List<dynamic>? ?? const [])
            .map((e) => CompletenessItem.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

class CompletenessItem {
  final String check;
  final String status; // Missing | Partial | Complete
  final int points;
  final int maxPoints;
  final int? detail;

  const CompletenessItem({
    required this.check,
    required this.status,
    required this.points,
    required this.maxPoints,
    this.detail,
  });

  factory CompletenessItem.fromJson(Map<String, dynamic> j) => CompletenessItem(
        check: (j['check'] ?? '') as String,
        status: (j['status'] ?? 'Missing') as String,
        points: j['points'] as int? ?? 0,
        maxPoints: j['maxPoints'] as int? ?? 0,
        detail: j['detail'] as int?,
      );
}
