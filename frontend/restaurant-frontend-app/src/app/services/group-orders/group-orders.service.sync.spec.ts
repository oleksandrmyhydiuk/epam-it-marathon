import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { SKIP_ERROR_TOAST } from '@app/auth/skip-error-toast.context';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import { GROUP_ORDER_PREVIEW } from '@app/shared/mocks/group-orders/group-order.preview';
import { GroupOrdersService } from './group-orders.service';

const BASE_URL = 'https://api.test';

describe('GroupOrdersService · silent sync options (Level 3)', () => {
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

  it('getSessions$ sets SKIP_ERROR_TOAST to true when { silent: true } is passed', () => {
    service.getSessions$('group-1', { silent: true }).subscribe();

    const req = httpMock.expectOne(`${BASE_URL}/guest/groups/group-1/sessions`);
    expect(req.request.context.get(SKIP_ERROR_TOAST)).toBeTrue();
    req.flush(GROUP_ORDER_PREVIEW.sessions);
  });

  it('getStatus$ sets SKIP_ERROR_TOAST to true when { silent: true } is passed', () => {
    service.getStatus$('group-1', { silent: true }).subscribe();

    const req = httpMock.expectOne(`${BASE_URL}/guest/groups/group-1/status`);
    expect(req.request.context.get(SKIP_ERROR_TOAST)).toBeTrue();
    req.flush({ status: 'Open' });
  });

  it('keeps SKIP_ERROR_TOAST false when silent option is omitted', () => {
    service.getSessions$('group-1').subscribe();

    const req = httpMock.expectOne(`${BASE_URL}/guest/groups/group-1/sessions`);
    expect(req.request.context.get(SKIP_ERROR_TOAST)).toBeFalse();
    req.flush(GROUP_ORDER_PREVIEW.sessions);
  });
});

