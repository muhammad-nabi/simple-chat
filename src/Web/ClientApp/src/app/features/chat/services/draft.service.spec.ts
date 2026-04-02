import { TestBed } from '@angular/core/testing';
import { DraftService } from './draft.service';

describe('DraftService', () => {
  let service: DraftService;
  let mockStorage: Record<string, string>;

  beforeEach(() => {
    mockStorage = {};

    jest.spyOn(Storage.prototype, 'setItem').mockImplementation(
      (key: string, value: string) => { mockStorage[key] = value; }
    );
    jest.spyOn(Storage.prototype, 'getItem').mockImplementation(
      (key: string) => mockStorage[key] ?? null
    );
    jest.spyOn(Storage.prototype, 'removeItem').mockImplementation(
      (key: string) => { delete mockStorage[key]; }
    );

    TestBed.configureTestingModule({});
    service = TestBed.inject(DraftService);
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('saveDraft', () => {
    it('should save draft to localStorage', () => {
      service.saveDraft(1, 'Hello draft');

      expect(mockStorage['draft:1']).toBeTruthy();
      const entry = JSON.parse(mockStorage['draft:1']);
      expect(entry.text).toBe('Hello draft');
      expect(entry.timestamp).toBeDefined();
    });

    it('should emit draft text via draft$ observable', () => {
      let draft: string | null = null;
      service.draft$.subscribe(d => draft = d);

      service.saveDraft(1, 'Hello');

      expect(draft).toBe('Hello');
    });

    it('should clear draft when saving empty text', () => {
      service.saveDraft(1, 'Previous');
      service.saveDraft(1, '  ');

      expect(mockStorage['draft:1']).toBeUndefined();
    });
  });

  describe('loadDraft', () => {
    it('should load draft from localStorage', () => {
      mockStorage['draft:1'] = JSON.stringify({ text: 'Saved draft', timestamp: 1000 });

      const result = service.loadDraft(1);

      expect(result).toBe('Saved draft');
    });

    it('should emit loaded draft via draft$ observable', () => {
      mockStorage['draft:1'] = JSON.stringify({ text: 'Saved', timestamp: 1000 });
      let draft: string | null = null;
      service.draft$.subscribe(d => draft = d);

      service.loadDraft(1);

      expect(draft).toBe('Saved');
    });

    it('should return null when no draft exists', () => {
      const result = service.loadDraft(99);
      expect(result).toBeNull();
    });

    it('should emit null via draft$ when no draft exists', () => {
      let draft: string | null = 'initial';
      service.draft$.subscribe(d => draft = d);

      service.loadDraft(99);

      expect(draft).toBeNull();
    });

    it('should return null for malformed JSON', () => {
      mockStorage['draft:1'] = 'not-json';

      const result = service.loadDraft(1);

      expect(result).toBeNull();
    });
  });

  describe('clearDraft', () => {
    it('should remove draft from localStorage', () => {
      mockStorage['draft:1'] = JSON.stringify({ text: 'Old', timestamp: 1000 });

      service.clearDraft(1);

      expect(mockStorage['draft:1']).toBeUndefined();
    });

    it('should emit null via draft$ observable', () => {
      let draft: string | null = 'old';
      service.draft$.subscribe(d => draft = d);

      service.clearDraft(1);

      expect(draft).toBeNull();
    });
  });
});
