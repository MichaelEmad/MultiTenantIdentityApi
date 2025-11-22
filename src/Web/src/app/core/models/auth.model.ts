export interface LoginRequest {
  email: string;
  password: string;
  rememberMe?: boolean;
}

export interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
  firstName?: string;
  lastName?: string;
  userName?: string;
}

export interface AuthResponse {
  succeeded: boolean;
  accessToken?: string;
  refreshToken?: string;
  accessTokenExpiration?: Date;
  refreshTokenExpiration?: Date;
  user?: UserDto;
  errors?: string[];
}

export interface UserDto {
  id: string;
  email: string;
  userName?: string;
  firstName?: string;
  lastName?: string;
  tenantId?: string;
  roles: string[];
}

export interface RefreshTokenRequest {
  accessToken: string;
  refreshToken: string;
}
