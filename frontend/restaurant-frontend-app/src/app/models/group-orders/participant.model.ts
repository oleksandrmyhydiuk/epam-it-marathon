export type ParticipantType = 'Owner' | 'Guest';

export interface ParticipantDish {
  id: string;
  name: string;
  price: number;
  imageUrl?: string | null;
}

export interface ParticipantItem {
  dish: ParticipantDish;
  quantity: number;
}

export interface Participant {
  id: string;
  type: ParticipantType;
  name: string;
  items: ParticipantItem[];
  total: number;
  isSubmitted: boolean;
}

export interface GroupOrderSessionsResponse {
  participants: Participant[];
  total: number;
}

export interface GuestSessionIdResponse {
  guestSessionId: string;
}

export type GroupOrderStatus = 'Open' | 'Submitted' | 'Closed';

export interface GroupOrderStatusResponse {
  status: GroupOrderStatus;
}

export interface SubmitGroupOrderRequest {
  reservationId: string;
}

export interface SubmittedDishItem {
  dishId: string;
  quantity: number;
}

