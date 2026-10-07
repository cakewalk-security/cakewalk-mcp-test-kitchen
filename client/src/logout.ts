export function logoutToLoginPage(): void {
  const form = document.createElement("form");
  form.method = "POST";
  form.action = "/Account/Logout";
  document.body.appendChild(form);
  form.submit();
}
