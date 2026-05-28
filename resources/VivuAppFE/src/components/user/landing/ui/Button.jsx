export default function Button({
  children,
  variant = "primary",
  className = "",
  ...props
}) {
  const base =
    "px-6 py-3 rounded-full font-semibold transition inline-flex items-center justify-center";

  const variants = {
    primary: "bg-[var(--primary)] text-white hover:bg-[var(--secondary)]",
    outline:
      "border border-[var(--primary)] text-[var(--primary)] hover:bg-[var(--primary)] hover:text-white",
  };

  return (
    <button className={`${base} ${variants[variant]} ${className}`} {...props}>
      {children}
    </button>
  );
}
