import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { of } from 'rxjs';
import { GroupOrdersService } from '@app/services/group-orders/group-orders.service';
import { GuestInviteService } from '@app/services/reservation/guest-invite.service';
import { ToastService } from '@app/services/toast/toast.service';
import { GROUP_ORDER_PREVIEW } from '@app/shared/mocks/group-orders/group-order.preview';
import { GroupOrderPageComponent } from './group-order-page.component';

describe('GroupOrderPageComponent · Remove Guest (Level 3 Bonus)', () => {
  let fixture: ComponentFixture<GroupOrderPageComponent>;
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
      of({ token: GROUP_ORDER_PREVIEW.inviteToken })
    );
    guestInviteService.getInvite$.and.returnValue(of(GROUP_ORDER_PREVIEW.invite));
    groupOrdersService.getSessions$.and.returnValue(of(GROUP_ORDER_PREVIEW.sessions));
    groupOrdersService.getOwnerSessionId$.and.returnValue(
      of({ guestSessionId: GROUP_ORDER_PREVIEW.guestSessionId })
    );
    groupOrdersService.getStatus$.and.returnValue(of({ status: 'Open' }));

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
    element = fixture.nativeElement as HTMLElement;
  });

  afterEach(() => {
    fixture.destroy();
  });

  it('shows Remove Guest button with aria-label on Guest cards only, not on Owner card', () => {
    fixture.detectChanges();

    const removeButtons = Array.from(
      element.querySelectorAll('app-participant-card button.remove-btn')
    ) as HTMLButtonElement[];

    expect(removeButtons.length).toBe(3);
    expect(removeButtons.map((btn) => btn.getAttribute('aria-label'))).toEqual([
      'Remove Kateryna',
      'Remove Olena',
      'Remove Andrii',
    ]);
  });

  it('hides Remove Guest buttons when group status is Submitted', () => {
    groupOrdersService.getStatus$.and.returnValue(of({ status: 'Submitted' }));
    fixture.detectChanges();

    const removeButtons = element.querySelectorAll('app-participant-card button.remove-btn');
    expect(removeButtons.length).toBe(0);
  });

  it('opens Remove Guest confirmation modal with guest name when Remove Guest is clicked', () => {
    fixture.detectChanges();

    const removeKaterynaBtn = element.querySelector(
      'button[aria-label="Remove Kateryna"]'
    ) as HTMLButtonElement;
    removeKaterynaBtn.click();
    fixture.detectChanges();

    const modal = element.querySelector('app-modal');
    expect(modal).not.toBeNull();
    expect(modal?.textContent).toContain(
      'Are you sure you want to remove Kateryna and their pre-order from this reservation? This action cannot be undone.'
    );
  });

  it('closes Remove Guest modal without calling removeSession$ when Cancel is clicked', () => {
    fixture.detectChanges();

    const removeBtn = element.querySelector(
      'button[aria-label="Remove Kateryna"]'
    ) as HTMLButtonElement;
    removeBtn.click();
    fixture.detectChanges();

    const cancelBtn = element.querySelector('.modal-cancel-btn') as HTMLButtonElement;
    cancelBtn.click();
    fixture.detectChanges();

    expect(element.querySelector('app-modal')).toBeNull();
    expect(groupOrdersService.removeSession$).not.toHaveBeenCalled();
  });

  it('calls removeSession$ and updates participants and Grand Total when confirmed', () => {
    fixture.detectChanges();

    const remainingParticipants = GROUP_ORDER_PREVIEW.sessions.participants.filter(
      (p) => p.id !== 'guest-session-1'
    );
    groupOrdersService.removeSession$.and.returnValue(
      of({
        participants: remainingParticipants,
        total: 400,
      })
    );

    const removeKaterynaBtn = element.querySelector(
      'button[aria-label="Remove Kateryna"]'
    ) as HTMLButtonElement;
    removeKaterynaBtn.click();
    fixture.detectChanges();

    const confirmBtn = element.querySelector('.modal-danger-btn') as HTMLButtonElement;
    confirmBtn.click();
    fixture.detectChanges();

    expect(groupOrdersService.removeSession$).toHaveBeenCalledWith(
      'demo-group-reservation',
      'guest-session-1'
    );
    expect(element.querySelectorAll('app-participant-card').length).toBe(3);
    expect(element.querySelector('.grand-total-amount')?.textContent?.trim()).toBe('$180.00');
  });
});
