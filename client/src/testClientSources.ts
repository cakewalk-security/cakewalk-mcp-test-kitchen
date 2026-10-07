export const TEST_CLIENT_LANGUAGE_TYPESCRIPT = "typescript";
export const TEST_CLIENT_LANGUAGE_CSHARP = "csharp";
export const TEST_CLIENT_LANGUAGE_PYTHON = "python";

export const TYPESCRIPT_CLIENT_REPO_URL =
  "https://github.com/cakewalk-security/cakewalk-mcp-ts-test-client";

export type TestClientLanguage = {
  id: string;
  label: string;
  available: boolean;
  repoUrl?: string;
  comingSoonMessage?: string;
};

export function getTestClientLanguages(): TestClientLanguage[] {
  return [
    {
      id: TEST_CLIENT_LANGUAGE_TYPESCRIPT,
      label: "TypeScript",
      available: true,
      repoUrl: TYPESCRIPT_CLIENT_REPO_URL,
    },
    {
      id: TEST_CLIENT_LANGUAGE_CSHARP,
      label: "C#",
      available: false,
      comingSoonMessage: "C# reference client coming soon.",
    },
    {
      id: TEST_CLIENT_LANGUAGE_PYTHON,
      label: "Python",
      available: false,
      comingSoonMessage: "Python reference client coming soon.",
    },
  ];
}
