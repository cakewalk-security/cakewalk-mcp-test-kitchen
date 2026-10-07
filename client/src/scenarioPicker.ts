export function isValidScenarioParamsJson(value: string): boolean {
  if (!value.trim()) {
    return false;
  }

  try {
    const parsed = JSON.parse(value) as unknown;
    return typeof parsed === "object" && parsed !== null && !Array.isArray(parsed);
  } catch {
    return false;
  }
}
