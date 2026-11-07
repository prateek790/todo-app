export interface AuthUser {
  id: number;
  name: string;
  email: string;
  userType: string;
}

export interface AuthSession {
  accessToken: string;
  expiresAt: string;
  user: AuthUser;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest extends LoginRequest {
  name: string;
  mobile: string;
  gender: string;
  dob: string;
}
