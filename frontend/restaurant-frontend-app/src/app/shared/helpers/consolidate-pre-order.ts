import { Participant, ParticipantDish } from '@app/models/group-orders/participant.model';

export interface ConsolidatedOrderItem {
  dish: ParticipantDish;
  dishId: string;
  name: string;
  price: number;
  imageUrl?: string | null;
  quantity: number;
  total: number;
}

export interface ConsolidatedPreOrder {
  items: ConsolidatedOrderItem[];
  total: number;
  submittedCount: number;
  unsubmittedCount: number;
}

export function consolidatePreOrder(participants: readonly Participant[]): ConsolidatedPreOrder {
  const byDishId = new Map<string, ConsolidatedOrderItem>();
  let submittedCount = 0;
  let unsubmittedCount = 0;

  for (const participant of participants) {
    if (!participant.isSubmitted) {
      unsubmittedCount += 1;
      continue;
    }

    submittedCount += 1;

    for (const item of participant.items) {
      if (item.quantity <= 0) {
        continue;
      }

      const existing = byDishId.get(item.dish.id);
      if (existing) {
        const nextQty = existing.quantity + item.quantity;
        existing.quantity = nextQty;
        existing.total = existing.price * nextQty;
      } else {
        byDishId.set(item.dish.id, {
          dish: item.dish,
          dishId: item.dish.id,
          name: item.dish.name,
          price: item.dish.price,
          imageUrl: item.dish.imageUrl,
          quantity: item.quantity,
          total: item.dish.price * item.quantity,
        });
      }
    }
  }

  const items = Array.from(byDishId.values());
  const total = items.reduce((sum, item) => sum + item.total, 0);

  return {
    items,
    total,
    submittedCount,
    unsubmittedCount,
  };
}

