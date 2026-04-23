import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useMutation } from '@apollo/client';
import { REGISTER } from '@/graphql/queries';
import type { AuthPayloadGql } from '@/graphql/types';
import { useAuthStore } from './authStore';
import type { AuthUser } from './types';

// ─────────────────────────────────────────────────────────────────────────────
// RegisterPage — user registration form
// ─────────────────────────────────────────────────────────────────────────────

interface RegisterData {
  register: AuthPayloadGql;
}

interface RegisterVariables {
  email: string;
  password: string;
  fullName: string;
}

function validateEmail(value: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
}

export function RegisterPage() {
  const navigate = useNavigate();
  const { login } = useAuthStore();

  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [errorMsg, setErrorMsg] = useState('');

  const [registerMutation, { loading }] = useMutation<RegisterData, RegisterVariables>(REGISTER, {
    onCompleted: (data) => {
      const { token, expiresAt, user } = data.register;
      const authUser: AuthUser = {
        id: user.id,
        email: user.email,
        fullName: user.fullName,
        role: user.role as AuthUser['role'],
        tenantId: user.tenantId,
      };
      login(token, expiresAt, authUser);
      navigate('/', { replace: true });
    },
    onError: (error) => {
      setErrorMsg(error.message || 'Đăng ký thất bại. Vui lòng thử lại.');
    },
  });

  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setErrorMsg('');

    if (!fullName.trim()) {
      setErrorMsg('Vui lòng nhập họ tên.');
      return;
    }
    if (!validateEmail(email)) {
      setErrorMsg('Email không hợp lệ.');
      return;
    }
    if (password.length < 6) {
      setErrorMsg('Mật khẩu phải có ít nhất 6 ký tự.');
      return;
    }
    if (password !== confirmPassword) {
      setErrorMsg('Mật khẩu xác nhận không khớp.');
      return;
    }

    registerMutation({ variables: { fullName: fullName.trim(), email, password } });
  }

  return (
    <div style={styles.page}>
      <div style={styles.card}>
        {/* Logo */}
        <div style={styles.logoRow}>
          <span style={styles.logoIcon}>⬡</span>
          <span style={styles.logoText}>IOC</span>
        </div>

        <h1 style={styles.title}>Đăng ký</h1>
        <p style={styles.subtitle}>Tạo tài khoản mới</p>

        <form onSubmit={handleSubmit} style={styles.form} noValidate>
          <label style={styles.label} htmlFor="fullName">
            Họ và tên
          </label>
          <input
            id="fullName"
            type="text"
            autoComplete="name"
            value={fullName}
            onChange={(e) => setFullName(e.target.value)}
            style={styles.input}
            placeholder="Nguyễn Văn A"
            disabled={loading}
            required
          />

          <label style={styles.label} htmlFor="email">
            Email
          </label>
          <input
            id="email"
            type="email"
            autoComplete="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            style={styles.input}
            placeholder="user@company.com"
            disabled={loading}
            required
          />

          <label style={styles.label} htmlFor="password">
            Mật khẩu
          </label>
          <input
            id="password"
            type="password"
            autoComplete="new-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            style={styles.input}
            placeholder="Tối thiểu 6 ký tự"
            disabled={loading}
            required
          />

          <label style={styles.label} htmlFor="confirmPassword">
            Xác nhận mật khẩu
          </label>
          <input
            id="confirmPassword"
            type="password"
            autoComplete="new-password"
            value={confirmPassword}
            onChange={(e) => setConfirmPassword(e.target.value)}
            style={styles.input}
            placeholder="Nhập lại mật khẩu"
            disabled={loading}
            required
          />

          {errorMsg && <div style={styles.error}>{errorMsg}</div>}

          <button type="submit" style={loading ? styles.btnDisabled : styles.btn} disabled={loading}>
            {loading ? 'Đang đăng ký...' : 'Đăng ký'}
          </button>
        </form>

        <div style={styles.footer}>
          Đã có tài khoản?{' '}
          <Link to="/login" style={styles.link}>
            Đăng nhập
          </Link>
        </div>
      </div>
    </div>
  );
}

// ── Inline styles (dark theme) ─────────────────────────────────────────────

const styles: Record<string, React.CSSProperties> = {
  page: {
    minHeight: '100vh',
    backgroundColor: '#060d1a',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    padding: '24px 16px',
  },
  card: {
    backgroundColor: '#0f172a',
    border: '1px solid #1e293b',
    borderRadius: 12,
    padding: '40px 36px',
    width: '100%',
    maxWidth: 420,
    boxShadow: '0 25px 50px -12px rgba(0,0,0,0.5)',
  },
  logoRow: {
    display: 'flex',
    alignItems: 'center',
    gap: 10,
    marginBottom: 24,
    justifyContent: 'center',
  },
  logoIcon: {
    fontSize: 28,
    color: '#38bdf8',
  },
  logoText: {
    fontSize: 22,
    fontWeight: 700,
    color: '#f1f5f9',
    letterSpacing: 2,
  },
  title: {
    margin: '0 0 4px',
    fontSize: 22,
    fontWeight: 700,
    color: '#f1f5f9',
    textAlign: 'center',
  },
  subtitle: {
    margin: '0 0 28px',
    fontSize: 13,
    color: '#64748b',
    textAlign: 'center',
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: 4,
  },
  label: {
    fontSize: 13,
    fontWeight: 500,
    color: '#94a3b8',
    marginBottom: 4,
    marginTop: 12,
  },
  input: {
    backgroundColor: '#0a1628',
    border: '1px solid #1e293b',
    borderRadius: 8,
    color: '#f1f5f9',
    fontSize: 14,
    padding: '10px 14px',
    outline: 'none',
    transition: 'border-color 0.15s',
    width: '100%',
    boxSizing: 'border-box',
  },
  error: {
    marginTop: 12,
    padding: '10px 14px',
    backgroundColor: 'rgba(239,68,68,0.12)',
    border: '1px solid rgba(239,68,68,0.3)',
    borderRadius: 8,
    color: '#f87171',
    fontSize: 13,
  },
  btn: {
    marginTop: 20,
    padding: '11px 0',
    backgroundColor: '#0ea5e9',
    border: 'none',
    borderRadius: 8,
    color: '#fff',
    fontSize: 15,
    fontWeight: 600,
    cursor: 'pointer',
    transition: 'background-color 0.15s',
    width: '100%',
  },
  btnDisabled: {
    marginTop: 20,
    padding: '11px 0',
    backgroundColor: '#1e40af',
    border: 'none',
    borderRadius: 8,
    color: '#93c5fd',
    fontSize: 15,
    fontWeight: 600,
    cursor: 'not-allowed',
    width: '100%',
  },
  footer: {
    marginTop: 24,
    textAlign: 'center',
    fontSize: 13,
    color: '#64748b',
  },
  link: {
    color: '#38bdf8',
    textDecoration: 'none',
  },
};
