import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Participant } from '@app/models/group-orders/participant.model';
import { ParticipantCardComponent } from './participant-card.component';

const YEVHENII: Participant = {
  id: 'owner-session-1',
  type: 'Owner',
  name: 'Yevhenii',
  items: [
    {
      dish: {
        id: 'dish-1',
        name: 'Fresh Strawberry Mint Salad',
        price: 180,
        imageUrl: 'assets/img/dishes/fresh-strawberry-mint-salad.jpg',
      },
      quantity: 2,
    },
  ],
  total: 360,
  isSubmitted: true,
};

const OLENA: Participant = {
  id: 'guest-session-2',
  type: 'Guest',
  name: 'Olena',
  items: [
    {
      dish: {
        id: 'dish-3',
        name: 'Tom Yum',
        price: 220,
        imageUrl: null,
      },
      quantity: 1,
    },
  ],
  total: 220,
  isSubmitted: false,
};

const EMPTY_GUEST: Participant = {
  id: 'guest-session-3',
  type: 'Guest',
  name: 'Andrii',
  items: [],
  total: 0,
  isSubmitted: false,
};

describe('ParticipantCardComponent', () => {
  let fixture: ComponentFixture<ParticipantCardComponent>;
  let element: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ParticipantCardComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ParticipantCardComponent);
    element = fixture.nativeElement as HTMLElement;
  });

  describe('Крок 1 · Заголовок картки', () => {
    it('renders "Owner: Yevhenii" for Owner participant', () => {
      fixture.componentRef.setInput('participant', YEVHENII);
      fixture.detectChanges();

      const heading = element.querySelector('h2.title');
      expect(heading?.textContent?.trim()).toBe('Owner: Yevhenii');
    });

    it('renders "Guest: Olena" for Guest participant', () => {
      fixture.componentRef.setInput('participant', OLENA);
      fixture.detectChanges();

      const heading = element.querySelector('h2.title');
      expect(heading?.textContent?.trim()).toBe('Guest: Olena');
    });
  });

  describe('Крок 3 · Страви і Subtotal', () => {
    it('renders dish name, quantity, and calculated line price', () => {
      fixture.componentRef.setInput('participant', YEVHENII);
      fixture.detectChanges();

      expect(element.querySelector('.name')?.textContent?.trim()).toBe(
        'Fresh Strawberry Mint Salad',
      );
      expect(element.querySelector('.qty')?.textContent?.trim()).toContain('2');
      expect(element.querySelector('.price')?.textContent?.trim()).toBe('$360.00');
    });

    it('renders dish image with width="40", height="40", and loading="lazy" when imageUrl exists', () => {
      fixture.componentRef.setInput('participant', YEVHENII);
      fixture.detectChanges();

      const img = element.querySelector('img.avatar') as HTMLImageElement | null;
      expect(img).not.toBeNull();
      expect(img?.getAttribute('width')).toBe('40');
      expect(img?.getAttribute('height')).toBe('40');
      expect(img?.getAttribute('loading')).toBe('lazy');
    });

    it('renders initial letter fallback when dish has no imageUrl', () => {
      fixture.componentRef.setInput('participant', OLENA);
      fixture.detectChanges();

      const fallback = element.querySelector('.avatar.initial');
      expect(fallback?.textContent?.trim()).toBe('T');
    });

    it('renders "No dishes yet" when items list is empty and shows formatted Subtotal', () => {
      fixture.componentRef.setInput('participant', EMPTY_GUEST);
      fixture.detectChanges();

      expect(element.querySelector('.empty')?.textContent?.trim()).toBe('No dishes yet');
      expect(element.querySelector('.subtotal')?.textContent).toContain('Subtotal');
      expect(element.querySelector('.subtotal')?.textContent).toContain('$0.00');
    });
  });

  describe('Крок 4 · Статус кошика текстом', () => {
    it('shows "Submitted" chip when participant has submitted their cart', () => {
      fixture.componentRef.setInput('participant', YEVHENII);
      fixture.detectChanges();

      expect(element.querySelector('.chip.submitted')?.textContent?.trim()).toBe('Submitted');
    });

    it('shows "Still choosing" chip when participant has not submitted yet', () => {
      fixture.componentRef.setInput('participant', OLENA);
      fixture.detectChanges();

      expect(element.querySelector('.chip.pending')?.textContent?.trim()).toBe('Still choosing');
    });
  });
});
