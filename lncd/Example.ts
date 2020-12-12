import { BebopView } from "bebop";

export namespace LeanCode.Bebop.Example {
  export enum RoleName {
    User = 0,
    Admin = 1,
  }

  export interface IMeDTO {
    id: string;
    firstName: string;
    lastName: string;
  }

  export const MeDTO = {
    encode(message: IMeDTO): Uint8Array {
      const view = BebopView.getInstance();
      view.startWriting();
      this.encodeInto(message, view);
      return view.toArray();
    },

    encodeInto(message: IMeDTO, view: BebopView): void {
        view.writeGuid(message.id);
        view.writeString(message.firstName);
        view.writeString(message.lastName);
    },

    decode(buffer: Uint8Array): IMeDTO {
      const view = BebopView.getInstance();
      view.startReading(buffer);
      return this.readFrom(view);
    },

    readFrom(view: BebopView): IMeDTO {
        let field0: string;
        field0 = view.readGuid();
        let field1: string;
        field1 = view.readString();
        let field2: string;
        field2 = view.readString();
        let message: IMeDTO = {
          id: field0,
          firstName: field1,
          lastName: field2,
        };
        return message;
    },
  };

  export interface IMe extends IRemoteQuery<MeDTO> {
  }

  export const Me = {
    encode(message: IMe): Uint8Array {
      const view = BebopView.getInstance();
      view.startWriting();
      this.encodeInto(message, view);
      return view.toArray();
    },

    encodeInto(message: IMe, view: BebopView): void {
        const pos = view.reserveMessageLength();
        const start = view.length;
        view.writeByte(0);
        const end = view.length;
        view.fillMessageLength(pos, end - start);
    },

    decode(buffer: Uint8Array): IMe {
      const view = BebopView.getInstance();
      view.startReading(buffer);
      return this.readFrom(view);
    },

    readFrom(view: BebopView): IMe {
        let message: IMe = {};
        const length = view.readMessageLength();
        const end = view.index + length;
        while (true) {
          switch (view.readByte()) {
            case 0:
              return message;

            default:
              view.index = end;
              return message;
          }
        }
    },
  };

  export interface IChangeName extends IRemoteCommand {
    newFirstName?: string;
    newLastName?: string;
  }

  export const ChangeName = {
    encode(message: IChangeName): Uint8Array {
      const view = BebopView.getInstance();
      view.startWriting();
      this.encodeInto(message, view);
      return view.toArray();
    },

    encodeInto(message: IChangeName, view: BebopView): void {
        const pos = view.reserveMessageLength();
        const start = view.length;
        if (message.newFirstName != null) {
          view.writeByte(1);
          view.writeString(message.newFirstName);
        }
        if (message.newLastName != null) {
          view.writeByte(2);
          view.writeString(message.newLastName);
        }
        view.writeByte(0);
        const end = view.length;
        view.fillMessageLength(pos, end - start);
    },

    decode(buffer: Uint8Array): IChangeName {
      const view = BebopView.getInstance();
      view.startReading(buffer);
      return this.readFrom(view);
    },

    readFrom(view: BebopView): IChangeName {
        let message: IChangeName = {};
        const length = view.readMessageLength();
        const end = view.index + length;
        while (true) {
          switch (view.readByte()) {
            case 0:
              return message;

            case 1:
              message.newFirstName = view.readString();
              break;

            case 2:
              message.newLastName = view.readString();
              break;

            default:
              view.index = end;
              return message;
          }
        }
    },
  };

}
