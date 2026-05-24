import { motion } from "framer-motion";

const members = [
  { name: "Lê Minh Duy", role: "Trưởng nhóm / Fullstack Developer" },
  { name: "Nguyễn Mạnh Đức", role: "Backend Developer" },
  { name: "Phạm Chấn Hưng", role: "Frontend Developer" },
  { name: "Đàm Quang Sáng", role: "Database Designer" },
  { name: "Trần Đức Mạnh", role: "QA/QC Tester" }
];

export function AboutPage() {
  return (
    <motion.div 
      className="page narrow"
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: -20 }}
      transition={{ duration: 0.4 }}
    >
      <div className="center" style={{ marginBottom: '48px' }}>
        <h1 style={{ color: '#17202f', textTransform: 'uppercase' }}>Đội ngũ phát triển</h1>
        <p className="description" style={{ fontSize: '1.2rem' }}>Dự án Quản lý rạp chiếu phim Star Cinema - Lớp <strong>DH13C1</strong></p>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))', gap: '32px' }}>
        {members.map((member, index) => (
          <motion.div
            key={member.name}
            initial={{ opacity: 0, scale: 0.8 }}
            animate={{ opacity: 1, scale: 1 }}
            transition={{ delay: index * 0.15, duration: 0.5, type: 'spring', bounce: 0.4 }}
            style={{
              background: '#fff',
              border: '1px solid #dde3ea',
              borderRadius: '16px',
              padding: '32px',
              textAlign: 'center',
              boxShadow: '0 14px 32px rgba(33, 45, 65, .08)'
            }}
          >
            <img 
              src={`https://api.dicebear.com/7.x/adventurer/svg?seed=${member.name}`} 
              alt={member.name} 
              style={{ width: '120px', height: '120px', borderRadius: '50%', background: '#edf1f5', marginBottom: '16px' }}
            />
            <h3 style={{ margin: '0 0 8px 0', color: '#8cc63f', fontSize: '1.2rem' }}>{member.name}</h3>
            <p style={{ margin: '0', color: '#64748b' }}>{member.role}</p>
            <p style={{ marginTop: '12px', fontSize: '0.85rem', fontWeight: 700, color: '#17202f', background: '#f4f6f8', padding: '4px 12px', borderRadius: '99px', display: 'inline-block' }}>
              DH13C1
            </p>
          </motion.div>
        ))}
      </div>
    </motion.div>
  );
}
