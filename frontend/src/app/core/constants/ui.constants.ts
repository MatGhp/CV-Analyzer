/**
 * UI timing constants for consistent UX across the application
 */
export const UI_TIMING = {
  /** Duration to show "Copied!" feedback (milliseconds) */
  COPY_SUCCESS_DURATION: 2000,

  /** Duration to show copy error message (milliseconds) */
  COPY_ERROR_DURATION: 5000,

  /** Delay before scrolling to element (milliseconds) */
  SCROLL_DELAY: 100
} as const;

/**
 * User-facing error messages
 */
export const ERROR_MESSAGES = {
  /** Generic analysis failure message */
  ANALYSIS_FAILED: 'We couldn\'t complete the analysis. Please try again.',

  /** Clipboard copy failure message */
  COPY_FAILED: 'Failed to copy. Please select and copy manually.',

  /** User not authenticated error */
  NOT_AUTHENTICATED: 'User not authenticated'
} as const;

/**
 * Success messages
 */
export const SUCCESS_MESSAGES = {
  /** Clipboard copy success */
  COPIED: 'Copied!',

  /** Default copy button text */
  COPY_TO_CLIPBOARD: 'Copy to Clipboard'
} as const;
