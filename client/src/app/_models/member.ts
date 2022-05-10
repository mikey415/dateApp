import { Photo } from "./photo";

export interface Member {
  id: number;
  userName: string;
  age: number;
  knownAs: string;
  created: Date;
  lastActive: Date;
  gender: string;
  lookingFor: string;
  interests: string;
  city: string;
  country: string;
  photos: Photo[];
  photoUrl: string;
  introduction: string;
}
