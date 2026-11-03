import { Participant } from '@app/models/group-orders/participant.model';
import { consolidatePreOrder } from './consolidate-pre-order';

describe('consolidatePreOrder', () => {
  const CAESAR = {
    id: 'dish-caesar',
    name: 'Caesar Salad',
    price: 180,
    imageUrl: 'assets/img/dishes/fresh-strawberry-mint-salad.jpg',
  };
  const TOM_YUM = {
    id: 'dish-tom-yum',
    name: 'Tom Yum',
    price: 220,
    imageUrl: null,
  };

  it('returns empty items and zero total for empty participants list', () => {
    const result = consolidatePreOrder([]);
    expect(result.items).toEqual([]);
    expect(result.total).toBe(0);
    expect(result.submittedCount).toBe(0);
    expect(result.unsubmittedCount).toBe(0);
  });

  it('includes only submitted participants and merges identical dishes by dishId', () => {
    const participants: Participant[] = [
      {
        id: 'owner-1',
        type: 'Owner',
        name: 'Yevhenii',
        items: [{ dish: CAESAR, quantity: 1 }],
        total: 180,
        isSubmitted: true,
      },
      {
        id: 'guest-1',
        type: 'Guest',
        name: 'Kateryna',
        items: [{ dish: CAESAR, quantity: 2 }],
        total: 360,
        isSubmitted: true,
      },
      {
        id: 'guest-2',
        type: 'Guest',
        name: 'Olena',
        items: [{ dish: TOM_YUM, quantity: 1 }],
        total: 220,
        isSubmitted: false,
      },
    ];

    const result = consolidatePreOrder(participants);

    expect(result.items.length).toBe(1);
    expect(result.items[0].dishId).toBe('dish-caesar');
    expect(result.items[0].quantity).toBe(3);
    expect(result.items[0].total).toBe(540);
    expect(result.total).toBe(540);
    expect(result.submittedCount).toBe(2);
    expect(result.unsubmittedCount).toBe(1);
  });
});

