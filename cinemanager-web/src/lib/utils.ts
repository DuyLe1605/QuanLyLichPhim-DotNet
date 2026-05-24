export function formatCurrency(value: number) {
  return new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(value);
}

export function formatDateTime(value: string) {
  return new Intl.DateTimeFormat("vi-VN", { dateStyle: "short", timeStyle: "short" }).format(new Date(value));
}

export function todayInputValue() {
  return new Date().toISOString().slice(0, 10);
}

export function getImageUrl(path?: string) {
  if (!path) return "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?auto=format&fit=crop&w=800&q=80";
  if (path.startsWith("http") || path.startsWith("data:")) return path;
  
  const cleanPath = path.replace(/^(\/|Resources\/)+/, "");
  return `http://localhost:5217/Resources/${cleanPath}`;
}
