import type { Snack } from "../../lib/types";
import { getImageUrl, formatCurrency } from "../../lib/utils";
import { motion } from "framer-motion";

export function ComboCard({ snack }: { snack: Snack }) {
  const fallbackImg = "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?auto=format&fit=crop&w=400&q=80";

  return (
    <motion.article 
      className="combo-card"
      initial={{ opacity: 0, scale: 0.9 }}
      whileInView={{ opacity: 1, scale: 1 }}
      viewport={{ once: true }}
      transition={{ duration: 0.3 }}
      whileHover={{ scale: 1.03 }}
    >
      <img 
        src={getImageUrl(snack.imageUrl)} 
        alt={snack.name} 
        onError={(e) => { e.currentTarget.src = fallbackImg; }}
      />
      <div className="combo-info">
        <div className="combo-title">{snack.name}</div>
        <div className="combo-desc">{snack.description || "Combo siêu tiết kiệm"}</div>
        <div className="combo-title" style={{ color: '#c42033', marginTop: '8px', fontSize: '1.2rem' }}>
          {formatCurrency(snack.price)}
        </div>
      </div>
    </motion.article>
  );
}
