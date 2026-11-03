import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { of } from 'rxjs';
import { GroupOrdersService } from '@app/services/group-orders/group-orders.service';
import { GuestInviteService } from '@app/services/reservation/guest-invite.service';
import { ToastService } from '@app/services/toast/toast.service';
import { GROUP_ORDER_PREVIEW } from '@app/shared/mocks/group-orders/group-order.preview';
import { GroupOrderPageComponent } from './group-order-page.component';

describe('GroupOrderPageComponent (Level 1)', () => {
  let fixture: ComponentFixture<GroupOrderPageComponent>;
  let element: HTMLElement;

  beforeEach(async () => {
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
        {
          provide: GuestInviteService,
          useValue: {
            getInviteToken$: () => of({ token: GROUP_ORDER_PREVIEW.inviteToken }),
            getInvite$: () => of(GROUP_ORDER_PREVIEW.invite),
          },
        },
        {
          provide: GroupOrdersService,
          useValue: {
            getSessions$: () => of(GROUP_ORDER_PREVIEW.sessions),
            getOwnerSessionId$: () =>
              of({ guestSessionId: GROUP_ORDER_PREVIEW.guestSessionId }),
            getStatus$: () => of({ status: GROUP_ORDER_PREVIEW.status }),
            submit$: () => of([]),
          },
        },
        {
          provide: ToastService,
          useValue: { success: () => undefined, error: () => undefined },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(GroupOrderPageComponent);
    element = fixture.nativeElement as HTMLElement;
    fixture.detectChanges();
  });

  it('uses semantic <main>, single <h1>, and <ul>/<li> for participants', () => {
    expect(element.querySelector('main')).not.toBeNull();
    expect(element.querySelectorAll('h1').length).toBe(1);
    expect(element.querySelector('h1')?.textContent?.trim()).toBe('Group Pre-Order');
    expect(element.querySelectorAll('ul.participants > li').length).toBe(4);
  });

  it('renders address, date, time, and guest count in header', () => {
    expect(element.textContent).toContain('48 Rustaveli Avenue');
    expect(element.textContent).toContain('Oct 17, 2026');
    expect(element.textContent).toContain('12:00 - 14:00');
    expect(element.textContent).toContain('4 of 5 guests');
  });

  it('renders all 4 participant cards as <article> elements', () => {
    expect(element.querySelectorAll('app-participant-card article.card').length).toBe(4);
  });

  it('displays formatted Grand Total in page footer', () => {
    const grandTotal = element.querySelector('.grand-total-row');
    expect(grandTotal?.textContent).toContain('Grand Total');
    expect(grandTotal?.textContent).toContain('$');
  });

  it('renders Submit Pre-Order button', () => {
    const submitBtn = element.querySelector('.submit-btn');
    expect(submitBtn?.textContent?.trim()).toBe('Submit Pre-Order');
  });
});

