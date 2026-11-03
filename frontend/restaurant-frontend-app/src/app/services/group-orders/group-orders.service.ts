import { HttpClient, HttpContext } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { SKIP_ERROR_TOAST } from '@app/auth/skip-error-toast.context';
import {
  GroupOrderSessionsResponse,
  GroupOrderStatusResponse,
  GuestSessionIdResponse,
  SubmittedDishItem,
} from '@app/models/group-orders/participant.model';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';

export interface GroupOrderRequestOptions {
  silent?: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class GroupOrdersService {
  private readonly baseUrl = inject(API_BASE_URL);
  private readonly http = inject(HttpClient);

  getSessions$(
    groupReservationId: string,
    options?: GroupOrderRequestOptions
  ): Observable<GroupOrderSessionsResponse> {
    const url = `${this.baseUrl}/guest/groups/${encodeURIComponent(groupReservationId)}/sessions`;
    if (options?.silent) {
      return this.http.get<GroupOrderSessionsResponse>(url, {
        context: new HttpContext().set(SKIP_ERROR_TOAST, true),
      });
    }
    return this.http.get<GroupOrderSessionsResponse>(url);
  }

  getOwnerSessionId$(reservationId: string): Observable<GuestSessionIdResponse> {
    const url = `${this.baseUrl}/reservations/${encodeURIComponent(reservationId)}/guest-session-id`;
    return this.http.get<GuestSessionIdResponse>(url);
  }

  getGuestSessionId$(reservationId: string): Observable<GuestSessionIdResponse> {
    return this.getOwnerSessionId$(reservationId);
  }

  getStatus$(
    groupReservationId: string,
    options?: GroupOrderRequestOptions
  ): Observable<GroupOrderStatusResponse> {
    const url = `${this.baseUrl}/guest/groups/${encodeURIComponent(groupReservationId)}/status`;
    if (options?.silent) {
      return this.http.get<GroupOrderStatusResponse>(url, {
        context: new HttpContext().set(SKIP_ERROR_TOAST, true),
      });
    }
    return this.http.get<GroupOrderStatusResponse>(url);
  }

  submit$(groupReservationId: string, reservationId: string): Observable<SubmittedDishItem[]> {
    const url = `${this.baseUrl}/group-reservations/${encodeURIComponent(groupReservationId)}/submit`;
    return this.http.post<SubmittedDishItem[]>(url, { reservationId });
  }

  removeSession$(
    groupReservationId: string,
    guestSessionId: string
  ): Observable<GroupOrderSessionsResponse> {
    const url = `${this.baseUrl}/guest/groups/${encodeURIComponent(groupReservationId)}/sessions/${encodeURIComponent(guestSessionId)}`;
    return this.http.delete<GroupOrderSessionsResponse>(url);
  }
}
