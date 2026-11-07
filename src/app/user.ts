export interface User {
  id: number;
  name: string;
  email: string;
  mobile: string;
  gender: string;
  dob: Date;
  isActive: boolean;
  range?: number[];
  userType?: string;
}
