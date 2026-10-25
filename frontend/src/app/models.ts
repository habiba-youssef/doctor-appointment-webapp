// export allows other TS files to use it
export interface Doctor {
  id: number;
  firstName: string;
  lastName: string;
  specialty: string;
  gender: number;
  consultationFee: number;
}

export interface Patient {
  id?: number;
  firstName: string;
  lastName: string;
  gender: number;
  phoneNumber: string;
  dateOfBirth: string; // not Date because using date-selection calendar, the value you retrieve is a string
}

export interface Appointment {
  id: number;
  doctorId: number;
  patientId: number;
  startTime: string;
  isCancelled: boolean;
}

export interface TimeSlot {
  time: string;
  available: boolean;
}