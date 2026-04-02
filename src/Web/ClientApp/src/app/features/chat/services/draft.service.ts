import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

interface DraftEntry {
  text: string;
  timestamp: number;
}

@Injectable({ providedIn: 'root' })
export class DraftService {
  private readonly _draft$ = new BehaviorSubject<string | null>(null);
  private readonly STORAGE_PREFIX = 'draft:';

  readonly draft$: Observable<string | null> = this._draft$.asObservable();

  saveDraft(conversationId: number, text: string): void {
    if (!text.trim()) {
      this.clearDraft(conversationId);
      return;
    }

    const entry: DraftEntry = { text, timestamp: Date.now() };
    try {
      localStorage.setItem(
        this.STORAGE_PREFIX + conversationId,
        JSON.stringify(entry)
      );
    } catch {
      // Silently fail — draft persistence is non-critical
    }
    this._draft$.next(text);
  }

  loadDraft(conversationId: number): string | null {
    const raw = localStorage.getItem(this.STORAGE_PREFIX + conversationId);
    if (!raw) {
      this._draft$.next(null);
      return null;
    }

    try {
      const entry: DraftEntry = JSON.parse(raw) as DraftEntry;
      this._draft$.next(entry.text);
      return entry.text;
    } catch {
      this._draft$.next(null);
      return null;
    }
  }

  clearDraft(conversationId: number): void {
    localStorage.removeItem(this.STORAGE_PREFIX + conversationId);
    this._draft$.next(null);
  }
}
