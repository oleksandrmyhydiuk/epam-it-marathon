import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { GroupOrderSessionsResponse } from '@app/models/group-orders/participant.model';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import { GROUP_ORDER_PREVIEW } from '@app/shared/mocks/group-orders/group-order.preview';
import { GroupOrdersService } from './group-orders.service';

const BASE_URL = 'https://api.test';

describe('GroupOrdersService · removeSession$ (Level 3 Bonus)', () => {
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

  it('sends DELETE /guest/groups/{groupId}/sessions/{guestSessionId} and returns updated sessions snapshot', () => {
    const updatedSnapshot: GroupOrderSessionsResponse = {
      participants: GROUP_ORDER_PREVIEW.sessions.participants.slice(0, 2),
      total: 540,
    };

    let actual: GroupOrderSessionsResponse | undefined;
    service
      .removeSession$('demo-group-reservation', 'guest-session-2')
      .subscribe((res) => (actual = res));

    const req = httpMock.expectOne(
      `${BASE_URL}/guest/groups/demo-group-reservation/sessions/guest-session-2`,
    );
    expect(req.request.method).toBe('DELETE');
    req.flush(updatedSnapshot);

    expect(actual).toEqual(updatedSnapshot);
  });
});
