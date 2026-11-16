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
  civilId: string;
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
  type: AppointmentType
}

export interface TimeSlot {
  time: string;
  available: boolean;
}

export enum AppointmentType {
    Consultation = 0,
    FollowUp = 1,
    Procedure = 2
}

export interface RescheduleForm {
  appointmentId: number;
  doctorId: number;
  date: string;
  time: string | null;
}