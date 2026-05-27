import { TestBed, ComponentFixture } from '@angular/core/testing';
import { PasswordChecklistComponent } from './password-checklist.component';

describe('PasswordChecklistComponent', () => {
  let fixture: ComponentFixture<PasswordChecklistComponent>;

  function create(password: string) {
    fixture = TestBed.createComponent(PasswordChecklistComponent);
    fixture.componentRef.setInput('password', password);
    fixture.detectChanges();
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PasswordChecklistComponent],
    }).compileComponents();
  });

  it('should create', () => {
    create('');
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('renders 4 rule items', () => {
    create('');
    const items = fixture.nativeElement.querySelectorAll('.checklist li');
    expect(items.length).toBe(4);
  });

  it('all rules are neutral (no pass/err class) when password is empty', () => {
    create('');
    const items: NodeListOf<HTMLElement> = fixture.nativeElement.querySelectorAll('.checklist li');
    items.forEach(li => {
      expect(li.classList.contains('rule-ok')).toBe(false);
      expect(li.classList.contains('rule-err')).toBe(false);
    });
  });

  it('applies rule-err to all rules when password is too short and has no special chars', () => {
    create('a');
    fixture.detectChanges();
    const items: NodeListOf<HTMLElement> = fixture.nativeElement.querySelectorAll('.checklist li');
    // 'a' → fails minLength, uppercase, digit, special → all rule-err
    items.forEach(li => expect(li.classList.contains('rule-err')).toBe(true));
  });

  it('applies rule-ok to minLength when password has 8+ chars', () => {
    create('abcdefgh');
    fixture.detectChanges();
    const minLengthLi: HTMLElement = fixture.nativeElement.querySelector('.checklist li:first-child');
    expect(minLengthLi.classList.contains('rule-ok')).toBe(true);
  });

  it('applies rule-ok to uppercase rule when password contains uppercase', () => {
    create('A');
    fixture.detectChanges();
    const items: NodeListOf<HTMLElement> = fixture.nativeElement.querySelectorAll('.checklist li');
    const uppercaseLi = items[1];
    expect(uppercaseLi.classList.contains('rule-ok')).toBe(true);
  });

  it('applies rule-ok to digit rule when password contains a digit', () => {
    create('1');
    fixture.detectChanges();
    const items: NodeListOf<HTMLElement> = fixture.nativeElement.querySelectorAll('.checklist li');
    const digitLi = items[2];
    expect(digitLi.classList.contains('rule-ok')).toBe(true);
  });

  it('applies rule-ok to special rule when password contains a special char', () => {
    create('!');
    fixture.detectChanges();
    const items: NodeListOf<HTMLElement> = fixture.nativeElement.querySelectorAll('.checklist li');
    const specialLi = items[3];
    expect(specialLi.classList.contains('rule-ok')).toBe(true);
  });

  it('marks all rules as rule-ok for a fully valid password', () => {
    create('Admin1234!');
    fixture.detectChanges();
    const items: NodeListOf<HTMLElement> = fixture.nativeElement.querySelectorAll('.checklist li');
    items.forEach(li => {
      expect(li.classList.contains('rule-ok')).toBe(true);
      expect(li.classList.contains('rule-err')).toBe(false);
    });
  });

  it('has aria-label on the checklist', () => {
    create('');
    const ul: HTMLElement = fixture.nativeElement.querySelector('.checklist');
    expect(ul.getAttribute('aria-label')).toBe('Cerințe parolă');
  });
});
