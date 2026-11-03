import {
  HttpErrorResponse,
  HttpHandlerFn,
  HttpInterceptorFn,
  HttpRequest,
  HttpResponse,
} from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';
import {
  GroupOrderSessionsResponse,
  GroupOrderStatus,
  Participant,
  SubmittedDishItem,
} from '@app/models/group-orders/participant.model';
import { consolidatePreOrder } from '@app/shared/helpers/consolidate-pre-order';
import { GROUP_ORDER_PREVIEW } from './group-order.preview';
import { DEMO_ID_TOKEN } from '../auth/demo-auth';

interface DemoGroupOrderState {
  status: GroupOrderStatus;
  participants: Participant[];
}

function createInitialState(): DemoGroupOrderState {
  return {
    status: GROUP_ORDER_PREVIEW.status,
    participants: GROUP_ORDER_PREVIEW.sessions.participants.map((participant) => ({
      ...participant,
      items: participant.items.map((item) => ({
        ...item,
        dish: { ...item.dish },
      })),
    })),
  };
}

let demoState: DemoGroupOrderState = createInitialState();

export function resetDemoGroupOrderState(): void {
  demoState = createInitialState();
}

function buildSessionsSnapshot(participants: Participant[]): GroupOrderSessionsResponse {
  const total = participants.reduce((sum, participant) => sum + participant.total, 0);
  return {
    participants: participants.map((participant) => ({
      ...participant,
      items: participant.items.map((item) => ({
        ...item,
        dish: { ...item.dish },
      })),
    })),
    total,
  };
}

export const groupOrderMockInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<any> => {
  const { url, method } = req;

  if (method === 'GET' && url.endsWith('/me')) {
    const authHeader = req.headers.get('Authorization') ?? '';
    if (authHeader.includes(DEMO_ID_TOKEN)) {
      return of(
        new HttpResponse({
          status: 200,
          body: {
            firstName: 'Yevhenii',
            lastName: 'Owner',
            email: 'demo@epam.com',
            role: 'Customer',
          },
        })
      );
    }
  }

  if (
    method === 'GET' &&
    url.endsWith(`/reservations/${GROUP_ORDER_PREVIEW.reservationId}/invite-token`)
  ) {
    return of(
      new HttpResponse({
        status: 200,
        body: { token: GROUP_ORDER_PREVIEW.inviteToken },
      })
    );
  }

  if (method === 'GET' && url.endsWith(`/guest/invites/${GROUP_ORDER_PREVIEW.inviteToken}`)) {
    return of(
      new HttpResponse({
        status: 200,
        body: { ...GROUP_ORDER_PREVIEW.invite },
      })
    );
  }

  if (
    method === 'GET' &&
    url.endsWith(`/reservations/${GROUP_ORDER_PREVIEW.reservationId}/guest-session-id`)
  ) {
    return of(
      new HttpResponse({
        status: 200,
        body: { guestSessionId: GROUP_ORDER_PREVIEW.guestSessionId },
      })
    );
  }

  const groupId = GROUP_ORDER_PREVIEW.invite.GroupReservationId;

  if (method === 'GET' && url.endsWith(`/guest/groups/${groupId}/sessions`)) {
    return of(
      new HttpResponse({
        status: 200,
        body: buildSessionsSnapshot(demoState.participants),
      })
    );
  }

  if (method === 'GET' && url.endsWith(`/guest/groups/${groupId}/status`)) {
    return of(
      new HttpResponse({
        status: 200,
        body: { status: demoState.status },
      })
    );
  }

  if (method === 'POST' && url.endsWith(`/group-reservations/${groupId}/submit`)) {
    const consolidated = consolidatePreOrder(demoState.participants);
    if (consolidated.submittedCount === 0) {
      return throwError(
        () =>
          new HttpErrorResponse({
            status: 400,
            statusText: 'Bad Request',
            error: { message: 'No submitted sessions found' },
          })
      );
    }
    if (consolidated.items.length === 0) {
      return throwError(
        () =>
          new HttpErrorResponse({
            status: 400,
            statusText: 'Bad Request',
            error: { message: 'No dishes in submitted sessions' },
          })
      );
    }

    demoState = {
      ...demoState,
      status: 'Submitted',
    };

    const responseBody: SubmittedDishItem[] = consolidated.items.map((item) => ({
      dishId: item.dishId,
      quantity: item.quantity,
    }));

    return of(
      new HttpResponse({
        status: 200,
        body: responseBody,
      })
    );
  }

  const deletePrefix = `/guest/groups/${groupId}/sessions/`;
  if (method === 'DELETE' && url.includes(deletePrefix)) {
    const guestSessionId = decodeURIComponent(
      url.slice(url.indexOf(deletePrefix) + deletePrefix.length)
    );
    const target = demoState.participants.find((participant) => participant.id === guestSessionId);
    if (!target) {
      return throwError(
        () =>
          new HttpErrorResponse({
            status: 404,
            statusText: 'Not Found',
            error: { message: 'Guest session not found' },
          })
      );
    }

    demoState = {
      ...demoState,
      participants: demoState.participants.filter(
        (participant) => participant.id !== guestSessionId
      ),
    };

    return of(
      new HttpResponse({
        status: 200,
        body: buildSessionsSnapshot(demoState.participants),
      })
    );
  }

  return next(req);
};
