import { RelativeTimePipe } from './relative-time.pipe';

describe('RelativeTimePipe', () => {
  let pipe: RelativeTimePipe;

  beforeEach(() => {
    pipe = new RelativeTimePipe();
  });

  it('should return empty string for null', () => {
    expect(pipe.transform(null)).toBe('');
  });

  it('should return empty string for undefined', () => {
    expect(pipe.transform(undefined)).toBe('');
  });

  it('should return "just now" for less than 1 minute ago', () => {
    const now = new Date();
    expect(pipe.transform(now.toISOString())).toBe('just now');
  });

  it('should return minutes for 1-59 minutes ago', () => {
    const date = new Date(Date.now() - 5 * 60000);
    expect(pipe.transform(date.toISOString())).toBe('5m');
  });

  it('should return hours for 1-23 hours ago', () => {
    const date = new Date(Date.now() - 3 * 3600000);
    expect(pipe.transform(date.toISOString())).toBe('3h');
  });

  it('should return "Yesterday" for yesterday', () => {
    const today = new Date();
    const yesterday = new Date(today.getFullYear(), today.getMonth(), today.getDate() - 1, 12, 0, 0);
    expect(pipe.transform(yesterday.toISOString())).toBe('Yesterday');
  });

  it('should return day name for this week', () => {
    const days = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
    const threeDaysAgo = new Date();
    threeDaysAgo.setDate(threeDaysAgo.getDate() - 3);
    threeDaysAgo.setHours(12, 0, 0, 0);
    const result = pipe.transform(threeDaysAgo.toISOString());
    expect(days).toContain(result);
  });

  it('should return short date for older dates', () => {
    const oldDate = new Date(2026, 2, 15, 12, 0, 0); // Mar 15
    const result = pipe.transform(oldDate.toISOString());
    expect(result).toBe('Mar 15');
  });
});
