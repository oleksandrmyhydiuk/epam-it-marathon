import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import {
  GroupOrderSessionsResponse,
  GroupOrderStatusResponse,
  GuestSessionIdResponse,
  SubmittedDishItem,
} from '@app/models/group-orders/participant.model';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import { GROUP_ORDER_PREVIEW } from '@app/shared/mocks/group-orders/group-order.preview';
import { GroupOrdersService } from './group-orders.service';

const BASE_URL = 'https://api.test';

describe('GroupOrdersService', () => {
  let service: GroupOrdersService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: BASE_URL },
      ],
    });

    service = TestBed.inject(GroupOrdersService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('getSessions$ sends GET /guest/groups/{id}/sessions and returns sessions payload', () => {
    let actual: GroupOrderSessionsResponse | undefined;
    service.getSessions$('group-1').subscribe((res) => (actual = res));

    const req = httpMock.expectOne(`${BASE_URL}/guest/groups/group-1/sessions`);
    expect(req.request.method).toBe('GET');
    req.flush(GROUP_ORDER_PREVIEW.sessions);

    expect(actual).toEqual(GROUP_ORDER_PREVIEW.sessions);
  });

  it('getOwnerSessionId$ sends GET /reservations/{id}/guest-session-id and returns guestSessionId', () => {
    let actual: GuestSessionIdResponse | undefined;
    service.getOwnerSessionId$('res-1').subscribe((res) => (actual = res));

    const req = httpMock.expectOne(`${BASE_URL}/reservations/res-1/guest-session-id`);
    expect(req.request.method).toBe('GET');
    req.flush({ guestSessionId: 'owner-session-1' });

    expect(actual).toEqual({ guestSessionId: 'owner-session-1' });
  });

  it('getStatus$ sends GET /guest/groups/{id}/status and returns status', () => {
    let actual: GroupOrderStatusResponse | undefined;
    service.getStatus$('group-1').subscribe((res) => (actual = res));

    const req = httpMock.expectOne(`${BASE_URL}/guest/groups/group-1/status`);
    expect(req.request.method).toBe('GET');
    req.flush({ status: 'Open' });

    expect(actual).toEqual({ status: 'Open' });
  });

  it('submit$ sends POST /group-reservations/{id}/submit with reservationId body', () => {
    let actual: SubmittedDishItem[] | undefined;
    service.submit$('group-1', 'res-1').subscribe((res) => (actual = res));

    const req = httpMock.expectOne(`${BASE_URL}/group-reservations/group-1/submit`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ reservationId: 'res-1' });
    req.flush([{ dishId: 'dish-1', quantity: 2 }]);

    expect(actual).toEqual([{ dishId: 'dish-1', quantity: 2 }]);
  });

  it('propagates HTTP error when submit$ fails with 400', () => {
    let status: number | undefined;
    service.submit$('group-1', 'res-1').subscribe({
      error: (err) => (status = err.status),
    });

    const req = httpMock.expectOne(`${BASE_URL}/group-reservations/group-1/submit`);
    req.flush({ message: 'No submitted sessions' }, { status: 400, statusText: 'Bad Request' });

    expect(status).toBe(400);
  });
});

