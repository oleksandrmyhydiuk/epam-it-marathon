import { CurrencyPipe, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnDestroy,
  OnInit,
  signal,
} from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import {
  catchError,
  EMPTY,
  exhaustMap,
  filter,
  finalize,
  forkJoin,
  interval,
  Observable,
  Subscription,
  switchMap,
  tap,
} from 'rxjs';
import { ParticipantCardComponent } from '@app/components/group-orders/participant-card/participant-card.component';
import { GroupOrderStatus, Participant } from '@app/models/group-orders/participant.model';
import { GuestInvite } from '@app/models/reservations/guest-invite.model';
import { GroupOrdersService } from '@app/services/group-orders/group-orders.service';
import { GuestInviteService } from '@app/services/reservation/guest-invite.service';
import { ToastService } from '@app/services/toast/toast.service';
import { ModalComponent } from '@app/shared/custom-components/modal/modal.component';
import { consolidatePreOrder } from '@app/shared/helpers/consolidate-pre-order';

export type GroupOrderErrorType = 'not-found' | 'retryable' | null;

@Component({
  selector: 'app-group-order-page',
  imports: [CurrencyPipe, DatePipe, ParticipantCardComponent, ModalComponent],
  templateUrl: './group-order-page.component.html',
  styleUrl: './group-order-page.component.scss',
  changeDetection: ChangeDetectionStrategy.Eager,
})
export class GroupOrderPageComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly guestInviteService = inject(GuestInviteService);
  private readonly groupOrdersService = inject(GroupOrdersService);
  private readonly toastService = inject(ToastService);

  private pollingSub: Subscription | null = null;
  private loadSub: Subscription | null = null;
  private syncInFlight = false;

  readonly reservationId = signal<string>('');
  readonly groupReservationId = signal<string>('');
  readonly invite = signal<GuestInvite | null>(null);
  readonly participants = signal<Participant[]>([]);
  readonly guestSessionId = signal<string | null>(null);
  readonly status = signal<GroupOrderStatus>('Open');

  readonly loading = signal<boolean>(false);
  readonly errorType = signal<GroupOrderErrorType>(null);
  readonly isSubmitting = signal<boolean>(false);
  readonly showSubmitModal = signal<boolean>(false);

  readonly lastUpdatedAt = signal<Date | null>(null);
  readonly refreshFailed = signal<boolean>(false);

  readonly participantToRemove = signal<Participant | null>(null);
  readonly isRemoving = signal<boolean>(false);

  readonly consolidated = computed(() => consolidatePreOrder(this.participants()));
  readonly grandTotal = computed(() => this.consolidated().total);

  readonly statusBadgeLabel = computed(() =>
    this.status() === 'Open' ? 'Collecting pre-orders' : 'Sent to Restaurant'
  );

  readonly hasAnyDishes = computed(() =>
    this.participants().some((person) => person.items.length > 0)
  );

  readonly isSubmitDisabled = computed(
    () => this.status() !== 'Open' || this.consolidated().items.length === 0 || this.isSubmitting()
  );

  readonly canRemoveGuests = computed(() => this.status() === 'Open' && !this.isSubmitting());

  ngOnInit(): void {
    const idFromParam =
      this.route.snapshot.paramMap.get('reservationId') ??
      this.route.snapshot.paramMap.get('id') ??
      'demo-reservation';
    this.reservationId.set(idFromParam);
    this.loadGroupOrder();
  }

  ngOnDestroy(): void {
    this.stopPolling();
    this.loadSub?.unsubscribe();
  }

  loadGroupOrder(): void {
    const reservationId = this.reservationId();
    if (!reservationId) {
      this.errorType.set('not-found');
      return;
    }

    this.stopPolling();
    this.loadSub?.unsubscribe();

    this.loading.set(true);
    this.errorType.set(null);
    this.refreshFailed.set(false);

    this.loadSub = this.guestInviteService
      .getInviteToken$(reservationId)
      .pipe(
        switchMap(({ token }) => this.guestInviteService.getInvite$(token)),
        switchMap((invite) => {
          this.invite.set(invite);
          this.groupReservationId.set(invite.GroupReservationId);

          return forkJoin({
            sessions: this.groupOrdersService.getSessions$(invite.GroupReservationId),
            ownerSession: this.groupOrdersService.getOwnerSessionId$(reservationId),
            statusRes: this.groupOrdersService.getStatus$(invite.GroupReservationId),
          });
        }),
        finalize(() => {
          this.loading.set(false);
        })
      )
      .subscribe({
        next: ({ sessions, ownerSession, statusRes }) => {
          this.participants.set(sessions.participants);
          this.guestSessionId.set(ownerSession.guestSessionId);
          this.status.set(statusRes.status);
          this.lastUpdatedAt.set(new Date());
          this.refreshFailed.set(false);

          if (statusRes.status === 'Open') {
            this.startPolling();
          }
        },
        error: (error: unknown) => {
          const status = error instanceof HttpErrorResponse ? error.status : null;
          this.errorType.set(status === 404 ? 'not-found' : 'retryable');
        },
      });
  }

  refreshNow(): void {
    if (this.syncInFlight || !this.groupReservationId()) {
      return;
    }
    this.performSilentSync$().subscribe();
  }

  openSubmitModal(): void {
    if (this.isSubmitDisabled()) {
      return;
    }
    this.showSubmitModal.set(true);
  }

  closeSubmitModal(): void {
    this.showSubmitModal.set(false);
  }

  confirmSubmit(): void {
    if (this.isSubmitting() || this.isSubmitDisabled()) {
      return;
    }

    const groupId = this.groupReservationId();
    const reservationId = this.reservationId();
    if (!groupId || !reservationId) {
      return;
    }

    this.isSubmitting.set(true);
    this.showSubmitModal.set(false);

    this.groupOrdersService
      .submit$(groupId, reservationId)
      .pipe(
        finalize(() => {
          this.isSubmitting.set(false);
        })
      )
      .subscribe({
        next: () => {
          this.status.set('Submitted');
          this.stopPolling();
          this.toastService.success('Pre-order sent to the restaurant', 'Success');
        },
        error: () => {
          // Error toast is already handled by tokenInterceptor
        },
      });
  }

  openRemoveModal(participant: Participant): void {
    if (!this.canRemoveGuests() || participant.type !== 'Guest') {
      return;
    }
    this.participantToRemove.set(participant);
  }

  closeRemoveModal(): void {
    this.participantToRemove.set(null);
  }

  confirmRemoveGuest(): void {
    const target = this.participantToRemove();
    const groupId = this.groupReservationId();
    if (!target || !groupId || this.isRemoving()) {
      return;
    }

    this.isRemoving.set(true);

    this.groupOrdersService
      .removeSession$(groupId, target.id)
      .pipe(
        finalize(() => {
          this.isRemoving.set(false);
        })
      )
      .subscribe({
        next: (response) => {
          this.participants.set(response.participants);
          this.participantToRemove.set(null);
          this.lastUpdatedAt.set(new Date());
        },
        error: () => {
          // Error toast is handled by tokenInterceptor
        },
      });
  }

  private startPolling(): void {
    this.stopPolling();

    this.pollingSub = interval(15_000)
      .pipe(
        filter(() => this.status() === 'Open' && !this.syncInFlight && !!this.groupReservationId()),
        exhaustMap(() => this.performSilentSync$())
      )
      .subscribe();
  }

  private stopPolling(): void {
    this.pollingSub?.unsubscribe();
    this.pollingSub = null;
  }

  private performSilentSync$(): Observable<unknown> {
    const groupId = this.groupReservationId();
    if (!groupId || this.syncInFlight) {
      return EMPTY;
    }

    this.syncInFlight = true;

    return forkJoin({
      sessions: this.groupOrdersService.getSessions$(groupId, { silent: true }),
      statusRes: this.groupOrdersService.getStatus$(groupId, { silent: true }),
    }).pipe(
      tap(({ sessions, statusRes }) => {
        this.participants.set(sessions.participants);
        this.status.set(statusRes.status);
        this.lastUpdatedAt.set(new Date());
        this.refreshFailed.set(false);

        if (statusRes.status !== 'Open') {
          this.stopPolling();
        }
      }),
      catchError(() => {
        this.refreshFailed.set(true);
        return EMPTY;
      }),
      finalize(() => {
        this.syncInFlight = false;
      })
    );
  }
}
