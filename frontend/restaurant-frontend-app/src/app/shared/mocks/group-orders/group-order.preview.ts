import {
  GroupOrderSessionsResponse,
  GroupOrderStatus,
  Participant,
} from '@app/models/group-orders/participant.model';
import { GuestInvite } from '@app/models/reservations/guest-invite.model';

export interface GroupOrderPreviewData {
  reservationId: string;
  inviteToken: string;
  guestSessionId: string;
  status: GroupOrderStatus;
  invite: GuestInvite;
  sessions: GroupOrderSessionsResponse;
}

export const PREVIEW_PARTICIPANTS: Participant[] = [
  {
    id: 'owner-session-1',
    type: 'Owner',
    name: 'Yevhenii',
    items: [
      {
        dish: {
          id: 'dish-1',
          name: 'Fresh Strawberry Mint Salad',
          price: 180,
          imageUrl: 'assets/img/dishes/fresh-strawberry-mint-salad.jpg',
        },
        quantity: 1,
      },
    ],
    total: 180,
    isSubmitted: true,
  },
  {
    id: 'guest-session-1',
    type: 'Guest',
    name: 'Kateryna',
    items: [
      {
        dish: {
          id: 'dish-1',
          name: 'Fresh Strawberry Mint Salad',
          price: 180,
          imageUrl: 'assets/img/dishes/fresh-strawberry-mint-salad.jpg',
        },
        quantity: 1,
      },
      {
        dish: {
          id: 'dish-2',
          name: 'Green Bowl',
          price: 180,
          imageUrl: null,
        },
        quantity: 1,
      },
    ],
    total: 360,
    isSubmitted: true,
  },
  {
    id: 'guest-session-2',
    type: 'Guest',
    name: 'Olena',
    items: [
      {
        dish: {
          id: 'dish-3',
          name: 'Tom Yum',
          price: 220,
          imageUrl: null,
        },
        quantity: 1,
      },
    ],
    total: 220,
    isSubmitted: false,
  },
  {
    id: 'guest-session-3',
    type: 'Guest',
    name: 'Andrii',
    items: [],
    total: 0,
    isSubmitted: false,
  },
];

export const GROUP_ORDER_PREVIEW: GroupOrderPreviewData = {
  reservationId: 'demo-reservation',
  inviteToken: 'demo-invite-token',
  guestSessionId: 'owner-session-1',
  status: 'Open',
  invite: {
    GroupReservationId: 'demo-group-reservation',
    LocationId: 'loc-rustaveli-48',
    RestaurantName: 'Green & Tasty',
    LocationAddress: '48 Rustaveli Avenue',
    StartTime: '2026-10-17T12:00:00',
    EndTime: '2026-10-17T14:00:00',
    MaxGuestCount: 5,
    TableNumber: 4,
    InviterName: 'Yevhenii',
  },
  sessions: {
    participants: PREVIEW_PARTICIPANTS,
    total: 760,
  },
};

