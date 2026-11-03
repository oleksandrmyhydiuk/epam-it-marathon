import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';
import { GroupOrdersService } from '@app/services/group-orders/group-orders.service';
import { GuestInviteService } from '@app/services/reservation/guest-invite.service';
import { ToastService } from '@app/services/toast/toast.service';
import { GROUP_ORDER_PREVIEW } from '@app/shared/mocks/group-orders/group-order.preview';
import { GroupOrderPageComponent } from './group-order-page.component';

describe('GroupOrderPageComponent (Level 2)', () => {
  let fixture: ComponentFixture<GroupOrderPageComponent>;
  let component: GroupOrderPageComponent;
  let element: HTMLElement;
  let guestInviteService: jasmine.SpyObj<GuestInviteService>;
  let groupOrdersService: jasmine.SpyObj<GroupOrdersService>;
  let toastService: jasmine.SpyObj<ToastService>;

  beforeEach(async () => {
    guestInviteService = jasmine.createSpyObj<GuestInviteService>('GuestInviteService', [
      'getInviteToken$',
      'getInvite$',
    ]);
    groupOrdersService = jasmine.createSpyObj<GroupOrdersService>('GroupOrdersService', [
      'getSessions$',
      'getOwnerSessionId$',
      'getStatus$',
      'submit$',
      'removeSession$',
    ]);
    toastService = jasmine.createSpyObj<ToastService>('ToastService', ['success', 'error']);

    guestInviteService.getInviteToken$.and.returnValue(
      of({ token: GROUP_ORDER_PREVIEW.inviteToken }),
    );
    guestInviteService.getInvite$.and.returnValue(of(GROUP_ORDER_PREVIEW.invite));
    groupOrdersService.getSessions$.and.returnValue(of(GROUP_ORDER_PREVIEW.sessions));
    groupOrdersService.getOwnerSessionId$.and.returnValue(
      of({ guestSessionId: GROUP_ORDER_PREVIEW.guestSessionId }),
    );
    groupOrdersService.getStatus$.and.returnValue(of({ status: 'Open' }));
    groupOrdersService.submit$.and.returnValue(of([{ dishId: 'dish-1', quantity: 2 }]));

    await TestBed.configureTestingModule({
      imports: [GroupOrderPageComponent],
      providers: [
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: convertToParamMap({ reservationId: 'demo-reservation' }),
            },
          },
        },
        { provide: GuestInviteService, useValue: guestInviteService },
        { provide: GroupOrdersService, useValue: groupOrdersService },
        { provide: ToastService, useValue: toastService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(GroupOrderPageComponent);
    component = fixture.componentInstance;
    element = fixture.nativeElement as HTMLElement;
  });

  afterEach(() => {
    fixture.destroy();
  });

  it('loads invite token, invite details, sessions, owner session id, and status on init', () => {
    fixture.detectChanges();

    expect(guestInviteService.getInviteToken$).toHaveBeenCalledWith('demo-reservation');
    expect(guestInviteService.getInvite$).toHaveBeenCalledWith('demo-invite-token');
    expect(groupOrdersService.getSessions$).toHaveBeenCalledWith('demo-group-reservation');
    expect(groupOrdersService.getOwnerSessionId$).toHaveBeenCalledWith('demo-reservation');
    expect(groupOrdersService.getStatus$).toHaveBeenCalledWith('demo-group-reservation');
  });

  it('renders header with Group Pre-Order title, address, date/time, and "4 of 5 guests"', () => {
    fixture.detectChanges();

    expect(element.querySelector('h1')?.textContent?.trim()).toBe('Group Pre-Order');
    expect(element.textContent).toContain('48 Rustaveli Avenue');
    expect(element.textContent).toContain('4 of 5 guests');
  });

  it('shows "Collecting pre-orders" status badge when status is Open', () => {
    fixture.detectChanges();

    expect(element.querySelector('.status-badge')?.textContent?.trim()).toBe(
      'Collecting pre-orders',
    );
  });

  it('labels Owner own card as "My Pre-Order" when participant.id === guestSessionId', () => {
    fixture.detectChanges();

    const titles = Array.from(element.querySelectorAll('app-participant-card h2.title')).map(
      (node) => node.textContent?.trim(),
    );
    expect(titles[0]).toBe('My Pre-Order');
    expect(titles).toContain('Guest: Kateryna');
  });

  it('calculates Grand Total as $540.00 (consolidated submitted carts) and not $760.00', () => {
    fixture.detectChanges();

    const grandTotalText = element.querySelector('.grand-total-amount')?.textContent?.trim();
    expect(grandTotalText).toBe('$540.00');
    expect(grandTotalText).not.toBe('$760.00');
  });

  it('renders consolidated pre-order items merged by dishId', () => {
    fixture.detectChanges();

    const consolidatedSection = element.querySelector('.consolidated-section');
    expect(consolidatedSection).not.toBeNull();
    expect(consolidatedSection?.textContent).toContain('Fresh Strawberry Mint Salad');
    expect(consolidatedSection?.textContent).toContain('× 2');
    expect(consolidatedSection?.textContent).toContain('$360.00');
  });

  it('shows warning banner for unsubmitted participants', () => {
    fixture.detectChanges();

    const banner = element.querySelector('.warning-banner');
    expect(banner?.textContent).toContain(
      "2 participants haven't submitted their cart yet — their dishes won't be included in the order",
    );
  });

  it('shows loading state while initial request is pending', () => {
    guestInviteService.getInviteToken$.and.returnValue(new Observable());
    fixture.detectChanges();

    expect(component.loading()).toBeTrue();
    expect(element.textContent).toContain('Loading');
  });

  it('shows "Group reservation not found" when API returns 404', () => {
    guestInviteService.getInviteToken$.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 404, statusText: 'Not Found' })),
    );
    fixture.detectChanges();

    expect(element.textContent).toContain('Group reservation not found');
  });

  it('shows retryable error state with "Try again" button and reloads on click', () => {
    guestInviteService.getInviteToken$.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 500, statusText: 'Server Error' })),
    );
    fixture.detectChanges();

    const retryBtn = element.querySelector('.retry-btn') as HTMLButtonElement | null;
    expect(retryBtn?.textContent?.trim()).toBe('Try again');

    guestInviteService.getInviteToken$.and.returnValue(
      of({ token: GROUP_ORDER_PREVIEW.inviteToken }),
    );
    retryBtn?.click();
    fixture.detectChanges();

    expect(element.querySelectorAll('app-participant-card').length).toBe(4);
  });

  it('shows empty state message and disables Submit Pre-Order when no participant has dishes', () => {
    groupOrdersService.getSessions$.and.returnValue(
      of({
        participants: [
          {
            id: 'owner-session-1',
            type: 'Owner',
            name: 'Yevhenii',
            items: [],
            total: 0,
            isSubmitted: false,
          },
        ],
        total: 0,
      }),
    );
    fixture.detectChanges();

    expect(element.textContent).toContain("Participants haven't added any dishes yet");
    const submitBtn = element.querySelector('.submit-btn') as HTMLButtonElement | null;
    expect(submitBtn?.disabled).toBeTrue();
  });

  it('opens confirmation modal on Submit Pre-Order click, submits once, shows toast, and switches badge to Sent to Restaurant', () => {
    fixture.detectChanges();

    const submitBtn = element.querySelector('.submit-btn') as HTMLButtonElement;
    expect(submitBtn.textContent?.trim()).toBe('Submit Pre-Order');
    submitBtn.click();
    fixture.detectChanges();

    const modalSubmitBtn = element.querySelector('.modal-submit-btn') as HTMLButtonElement;
    const modalCancelBtn = element.querySelector('.modal-cancel-btn') as HTMLButtonElement;
    expect(modalSubmitBtn.textContent?.trim()).toBe('Submit');
    expect(modalCancelBtn.textContent?.trim()).toBe('Cancel');

    modalSubmitBtn.click();
    fixture.detectChanges();

    expect(groupOrdersService.submit$).toHaveBeenCalledTimes(1);
    expect(groupOrdersService.submit$).toHaveBeenCalledWith(
      'demo-group-reservation',
      'demo-reservation',
    );
    expect(toastService.success).toHaveBeenCalledWith(
      'Pre-order sent to the restaurant',
      'Success',
    );
    expect(element.querySelector('.status-badge')?.textContent?.trim()).toBe('Sent to Restaurant');
    expect(submitBtn.disabled).toBeTrue();
  });

  it('keeps screen usable and allows retrying submit after a submit error', () => {
    groupOrdersService.submit$.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 400, statusText: 'Bad Request' })),
    );
    fixture.detectChanges();

    (element.querySelector('.submit-btn') as HTMLButtonElement).click();
    fixture.detectChanges();
    (element.querySelector('.modal-submit-btn') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(component.status()).toBe('Open');
    expect((element.querySelector('.submit-btn') as HTMLButtonElement).disabled).toBeFalse();
    expect(toastService.error).not.toHaveBeenCalled();
  });
});
