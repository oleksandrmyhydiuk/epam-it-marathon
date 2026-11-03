import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, fakeAsync, TestBed, tick } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';
import { GroupOrdersService } from '@app/services/group-orders/group-orders.service';
import { GuestInviteService } from '@app/services/reservation/guest-invite.service';
import { ToastService } from '@app/services/toast/toast.service';
import { GROUP_ORDER_PREVIEW } from '@app/shared/mocks/group-orders/group-order.preview';
import { GroupOrderPageComponent } from './group-order-page.component';

describe('GroupOrderPageComponent · background sync (Level 3)', () => {
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
      of({ token: GROUP_ORDER_PREVIEW.inviteToken })
    );
    guestInviteService.getInvite$.and.returnValue(of(GROUP_ORDER_PREVIEW.invite));
    groupOrdersService.getSessions$.and.returnValue(of(GROUP_ORDER_PREVIEW.sessions));
    groupOrdersService.getOwnerSessionId$.and.returnValue(
      of({ guestSessionId: GROUP_ORDER_PREVIEW.guestSessionId })
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

  it('polls getSessions$ and getStatus$ with { silent: true } every 15 seconds while Open', fakeAsync(() => {
    fixture.detectChanges();
    expect(groupOrdersService.getSessions$).toHaveBeenCalledTimes(1);

    tick(15_000);
    fixture.detectChanges();

    expect(groupOrdersService.getSessions$).toHaveBeenCalledTimes(2);
    expect(groupOrdersService.getSessions$).toHaveBeenCalledWith('demo-group-reservation', {
      silent: true,
    });
    expect(groupOrdersService.getStatus$).toHaveBeenCalledWith('demo-group-reservation', {
      silent: true,
    });
  }));

  it('updates participants and Grand Total in place without full-screen loader and shows "Updated at"', fakeAsync(() => {
    fixture.detectChanges();
    expect(element.textContent).toContain('Updated at');

    const updatedParticipants = GROUP_ORDER_PREVIEW.sessions.participants.map((p) =>
      p.name === 'Olena' ? { ...p, isSubmitted: true } : p
    );
    groupOrdersService.getSessions$.and.returnValue(
      of({ participants: updatedParticipants, total: 760 })
    );

    tick(15_000);
    fixture.detectChanges();

    expect(component.loading()).toBeFalse();
    expect(element.querySelector('.grand-total-amount')?.textContent?.trim()).toBe('$760.00');
  }));

  it('triggers immediate silent sync when Refresh button is clicked', fakeAsync(() => {
    fixture.detectChanges();
    expect(groupOrdersService.getSessions$).toHaveBeenCalledTimes(1);

    const refreshBtn = element.querySelector('.refresh-btn') as HTMLButtonElement;
    expect(refreshBtn.textContent?.trim()).toBe('Refresh');
    refreshBtn.click();
    fixture.detectChanges();

    expect(groupOrdersService.getSessions$).toHaveBeenCalledTimes(2);
    expect(groupOrdersService.getSessions$).toHaveBeenCalledWith('demo-group-reservation', {
      silent: true,
    });
  }));

  it('keeps existing data and shows "Couldn\'t refresh. Showing data from …" when silent refresh fails', fakeAsync(() => {
    fixture.detectChanges();

    groupOrdersService.getSessions$.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 500, statusText: 'Server Error' }))
    );

    tick(15_000);
    fixture.detectChanges();

    expect(element.querySelectorAll('app-participant-card').length).toBe(4);
    expect(element.textContent).toContain("Couldn't refresh. Showing data from");
  }));

  it('does not start a new poll request while previous poll request is still in-flight (exhaustMap)', fakeAsync(() => {
    fixture.detectChanges();
    expect(groupOrdersService.getSessions$).toHaveBeenCalledTimes(1);

    groupOrdersService.getSessions$.and.returnValue(new Observable());
    tick(15_000);
    expect(groupOrdersService.getSessions$).toHaveBeenCalledTimes(2);

    tick(15_000);
    expect(groupOrdersService.getSessions$).toHaveBeenCalledTimes(2);
  }));

  it('stops polling after submit and does not create duplicate timers on reload', fakeAsync(() => {
    fixture.detectChanges();

    component.loadGroupOrder();
    fixture.detectChanges();

    const callsAfterReload = groupOrdersService.getSessions$.calls.count();
    tick(15_000);
    expect(groupOrdersService.getSessions$.calls.count()).toBe(callsAfterReload + 1);

    component.confirmSubmit();
    fixture.detectChanges();

    const callsAfterSubmit = groupOrdersService.getSessions$.calls.count();
    tick(30_000);
    expect(groupOrdersService.getSessions$.calls.count()).toBe(callsAfterSubmit);
  }));
});
